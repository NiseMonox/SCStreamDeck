using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SCStreamDeck.Logging;
using System.Diagnostics.CodeAnalysis;

namespace SCStreamDeck.Services.Audio;

/// <summary>
///     Audio player service using WASAPI shared mode with mixing support.
///     The output is opened on the current default playback device when a sound is played and closed again shortly
///     after the last sound has ended, so the plugin holds no audio stream while idle and follows device changes.
/// </summary>
public sealed class AudioPlayerService : IDisposable
{
    private static readonly TimeSpan s_idleTimeout = TimeSpan.FromSeconds(5);
    private static readonly WaveFormat s_mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    private readonly Timer _idleTimer;
    private readonly Lock _lock = new();
    private int _activeInputs;
    private MMDeviceEnumerator? _deviceEnumerator;
    private bool _disposed;
    private MixingSampleProvider? _mixer;
    private WasapiOut? _outputDevice;
    private string? _outputDeviceId;

    public AudioPlayerService() =>
        _idleTimer = new Timer(_ => CloseOutputWhenIdle(), null, Timeout.Infinite, Timeout.Infinite);

    /// <summary>
    ///     Disposes audio resources.
    ///     Marked as [ExcludeFromCodeCoverage] because:
    ///     - Depends on NAudio WASAPI audio device (MMDeviceEnumerator, WasapiOut)
    ///     - Integration testing requires audio system initialization
    ///     - Resource cleanup pattern verified manually
    /// </summary>
    [ExcludeFromCodeCoverage]
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            CloseOutput();
            _deviceEnumerator?.Dispose();
            _deviceEnumerator = null;
            _idleTimer.Dispose();

            _disposed = true;
        }
    }

    public void Play(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                MixingSampleProvider mixer = EnsureOutput();
                AudioReaderAdapter adapter = CreateReaderAdapter(filePath);

                _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                mixer.AddMixerInput(ConvertToMixerFormat(adapter));
                Interlocked.Increment(ref _activeInputs);
            }
            catch (Exception ex)
            {
                Log.Err($"[{nameof(AudioPlayerService)}] Failed to play audio: {ex.Message}", ex);
                throw;
            }
        }
    }

    /// <summary>
    ///     Returns the mixer of an output that is playing on the current default device, opening a new output when
    ///     there is none, the default device changed, or the previous output stopped (e.g. its device was removed).
    ///     Marked as [ExcludeFromCodeCoverage] because it depends on the WASAPI audio device.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private MixingSampleProvider EnsureOutput()
    {
        _deviceEnumerator ??= new MMDeviceEnumerator();
        MMDevice device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        if (_mixer != null &&
            _outputDevice is { PlaybackState: PlaybackState.Playing } &&
            string.Equals(device.ID, _outputDeviceId, StringComparison.Ordinal))
        {
            device.Dispose();
            return _mixer;
        }

        CloseOutput();

        WasapiOut output = new(device, AudioClientShareMode.Shared, false, 50);
        MixingSampleProvider mixer = new(s_mixerFormat) { ReadFully = true };
        try
        {
            output.Init(mixer);
            output.Play();
        }
        catch
        {
            output.Dispose();
            throw;
        }

        mixer.MixerInputEnded += OnMixerInputEnded;
        _outputDevice = output;
        _outputDeviceId = device.ID;
        _mixer = mixer;
        return mixer;
    }

    [ExcludeFromCodeCoverage]
    private void CloseOutput()
    {
        if (_mixer != null)
        {
            _mixer.MixerInputEnded -= OnMixerInputEnded;
            _mixer = null;
        }

        WasapiOut? output = _outputDevice;
        _outputDevice = null;
        _outputDeviceId = null;

        try
        {
            output?.Stop();
            output?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Debug($"[{nameof(AudioPlayerService)}] Failed to close audio output: {ex.Message}");
        }

        Interlocked.Exchange(ref _activeInputs, 0);
    }

    /// <summary>
    ///     Runs on the audio thread, so it only counts and (re)arms the idle timer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private void OnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        if (Interlocked.Decrement(ref _activeInputs) > 0)
        {
            return;
        }

        try
        {
            _idleTimer.Change(s_idleTimeout, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // The service was disposed while the last sound was ending.
        }
    }

    [ExcludeFromCodeCoverage]
    private void CloseOutputWhenIdle()
    {
        lock (_lock)
        {
            if (_disposed || Volatile.Read(ref _activeInputs) > 0)
            {
                return;
            }

            CloseOutput();
        }
    }

    private static AudioReaderAdapter CreateReaderAdapter(string filePath)
    {
        try
        {
            AudioFileReader reader = new(filePath);
            return new AudioReaderAdapter(reader);
        }
        catch (Exception ex)
        {
            Log.Debug(
                $"[{nameof(AudioPlayerService)}] AudioFileReader failed, falling back to MediaFoundationReader{Environment.NewLine}{ex}");
            MediaFoundationReader reader = new(filePath);
            ISampleProvider sampleProvider = reader.ToSampleProvider();
            return new AudioReaderAdapter(sampleProvider, reader);
        }
    }

    /// <summary>
    ///     Converts audio sample provider to mixer format (sample rate, channels).
    ///     Marked as [ExcludeFromCodeCoverage] because:
    ///     - Depends on NAudio sample providers and WASAPI output
    ///     - Integration testing requires audio device initialization
    ///     - Format conversion logic tested through Play() method integration
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static ISampleProvider ConvertToMixerFormat(AudioReaderAdapter source)
    {
        ISampleProvider sampleProvider = source;

        if (sampleProvider.WaveFormat.Channels == 1 && s_mixerFormat.Channels == 2)
        {
            sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
        }

        if (sampleProvider.WaveFormat.SampleRate != s_mixerFormat.SampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, s_mixerFormat.SampleRate);
        }

        return sampleProvider;
    }
}
