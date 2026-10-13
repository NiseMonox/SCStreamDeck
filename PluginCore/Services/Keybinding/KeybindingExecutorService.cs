using SCStreamDeck.Logging;
using SCStreamDeck.Models;
using SCStreamDeck.Services.Keybinding.ActivationHandlers;
using System.Collections.Concurrent;
using WindowsInput;

namespace SCStreamDeck.Services.Keybinding;

/// <summary>
///     Service for executing keybinding actions.
/// </summary>
public sealed class KeybindingExecutorService : IDisposable
{
    private static readonly TimeSpan s_oneShotHoldMargin = TimeSpan.FromMilliseconds(100);

    private readonly ConcurrentDictionary<string, Timer> _activationTimers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ActivationModeHandlerRegistry _handlerRegistry;
    private readonly ConcurrentDictionary<string, byte> _holdStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly KeybindingInputExecutor _inputExecutor;
    private readonly KeybindingLoaderService _loaderService;

    // Tail of the work queued per action, so a key's down and up run in the order they arrived.
    private readonly Dictionary<string, Task> _pendingByAction = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _pendingGate = new();
    private bool _disposed;

    public KeybindingExecutorService(
        KeybindingLoaderService loaderService,
        IInputSimulator inputSimulator)
    {
        _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
        ArgumentNullException.ThrowIfNull(inputSimulator);

        _handlerRegistry = new ActivationModeHandlerRegistry();
        _inputExecutor = new KeybindingInputExecutor(inputSimulator, _holdStates, _activationTimers);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Timer[] timers = [.. _activationTimers.Values];

        _activationTimers.Clear();

        foreach (Timer timer in timers)
        {
            timer.Dispose();
        }

        _holdStates.Clear();
        _disposed = true;
    }

    public async Task<bool> ExecuteAsync(KeybindingExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.IsValid(out string? errorMessage))
        {
            Log.Warn($"[{nameof(KeybindingExecutorService)}] Invalid execution context - {errorMessage}");

            return false;
        }

        try
        {
            return await RunInOrderAsync(context.ActionName, () => ExecuteWithActivationMode(context, cancellationToken))
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            Log.Err(
                $"[{nameof(KeybindingExecutorService)}] Operation failed for '{context.ActionName}'",
                ex);

            return false;
        }
    }

    internal async Task<bool> ExecutePressNoRepeatAsync(
        string actionName,
        string binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(binding);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await RunInOrderAsync(
                    actionName,
                    () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        ParsedInputResult? parsedInput = KeybindingParserService.ParseBinding(binding);
                        if (parsedInput == null)
                        {
                            Log.Warn($"[{nameof(KeybindingExecutorService)}] Failed to parse binding '{binding}'");
                            return false;
                        }

                        bool success =
                            _inputExecutor.ExecutePressNoRepeat(new ParsedInput
                            {
                                Type = parsedInput.Type,
                                Value = parsedInput.Value
                            });
                        if (!success)
                        {
                            Log.Warn(
                                $"[{nameof(KeybindingExecutorService)}] Failed to execute no-repeat binding '{binding}' for '{actionName}'");
                        }

                        return success;
                    })
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            Log.Err($"[{nameof(KeybindingExecutorService)}] Operation failed for '{actionName}'", ex);
            return false;
        }
    }

    /// <summary>
    ///     Performs one complete activation of an action from a single trigger (Toggle Key): the number of taps or the
    ///     hold duration its activation mode needs, instead of mirroring a physical key down and up.
    /// </summary>
    internal async Task<bool> ExecuteOneShotAsync(
        string actionName,
        string binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(binding);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await RunInOrderAsync(actionName, () => ExecuteOneShot(actionName, binding, cancellationToken))
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            Log.Err($"[{nameof(KeybindingExecutorService)}] Operation failed for '{actionName}'", ex);
            return false;
        }
    }

    private bool ExecuteOneShot(string actionName, string binding, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ParsedInputResult? parsedInput = KeybindingParserService.ParseBinding(binding);
        if (parsedInput == null)
        {
            Log.Warn($"[{nameof(KeybindingExecutorService)}] Failed to parse binding '{binding}'");
            return false;
        }

        ParsedInput input = new() { Type = parsedInput.Type, Value = parsedInput.Value };
        ActivationModeMetadata metadata = _loaderService.GetMetadata(actionName) ?? ActivationModeMetadata.Empty();

        if (metadata.MultiTap > 1)
        {
            return ImmediatePressHandler.ExecuteMultiTap(_inputExecutor, input, metadata.MultiTap);
        }

        if (metadata.PressTriggerThreshold <= 0)
        {
            return _inputExecutor.ExecutePressNoRepeat(input);
        }

        // Long-press modes only fire once the input has been held past their threshold.
        string holdKey = $"{actionName}|oneshot";
        if (!_inputExecutor.ExecuteDown(input, holdKey))
        {
            return false;
        }

        try
        {
            Thread.Sleep(TimeSpan.FromSeconds(metadata.PressTriggerThreshold) + s_oneShotHoldMargin);
        }
        finally
        {
            _inputExecutor.ExecuteUp(input, holdKey);
        }

        return true;
    }

    /// <summary>
    ///     Queues work behind whatever is still pending for the same action. Key down and key up arrive as separate
    ///     events; running each on its own thread-pool task could release a key before it was pressed.
    /// </summary>
    private Task<bool> RunInOrderAsync(string actionName, Func<bool> work)
    {
        lock (_pendingGate)
        {
            Task previous = _pendingByAction.GetValueOrDefault(actionName, Task.CompletedTask);
            Task<bool> current = previous.ContinueWith(
                static (_, state) => ((Func<bool>)state!)(),
                work,
                CancellationToken.None,
                TaskContinuationOptions.DenyChildAttach,
                TaskScheduler.Default);

            _pendingByAction[actionName] = current;
            _ = current.ContinueWith(
                completed => ForgetIfLast(actionName, completed),
                CancellationToken.None,
                TaskContinuationOptions.DenyChildAttach,
                TaskScheduler.Default);

            return current;
        }
    }

    private void ForgetIfLast(string actionName, Task completed)
    {
        lock (_pendingGate)
        {
            if (_pendingByAction.TryGetValue(actionName, out Task? last) && ReferenceEquals(last, completed))
            {
                _pendingByAction.Remove(actionName);
            }
        }
    }

    /// <summary>
    ///     Executes an action using the Strategy pattern via ActivationModeHandlerRegistry.
    ///     Each activation mode (press, hold, tap, etc.) has its own handler.
    /// </summary>
    private bool ExecuteWithActivationMode(KeybindingExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ParsedInputResult? parsedInput = KeybindingParserService.ParseBinding(context.Binding);
        if (parsedInput == null)
        {
            Log.Warn($"[{nameof(KeybindingExecutorService)}] Failed to parse binding '{context.Binding}'");

            return false;
        }

        // Get activation mode metadata for the specific action
        ActivationModeMetadata metadata = _loaderService.GetMetadata(context.ActionName) ?? ActivationModeMetadata.Empty();

        ActivationExecutionContext executionContext = new()
        {
            ActionName = context.ActionName,
            Input = new ParsedInput { Type = parsedInput.Type, Value = parsedInput.Value },
            IsKeyDown = context.IsKeyDown,
            Mode = context.ActivationMode,
            Metadata = metadata
        };

        return _handlerRegistry.Execute(executionContext, _inputExecutor);
    }
}
