using SCStreamDeck.Common;

namespace SCStreamDeck.Models;

/// <summary>
///     Represents a Star Citizen action with its bindings and metadata.
/// </summary>
public sealed class KeybindingAction
{
    public required string ActionName { get; init; }
    public required string MapName { get; init; }
    public string MapLabel { get; init; } = string.Empty;
    public string UiLabel { get; init; } = string.Empty;
    public string UiDescription { get; init; } = string.Empty;
    public string UiCategory { get; init; } = string.Empty;
    public string KeyboardBinding { get; init; } = string.Empty;
    public string MouseBinding { get; init; } = string.Empty;
    public string JoystickBinding { get; init; } = string.Empty;
    public string GamepadBinding { get; init; } = string.Empty;
    public ActivationMode ActivationMode { get; init; } = ActivationMode.press;
    public bool IsToggleCandidate { get; init; }

    /// <summary>
    ///     The binding the plugin can send for this action: its keyboard binding, otherwise a mouse button or wheel
    ///     binding. Null when the action is unbound or only bound to an axis (which cannot be sent as a press).
    /// </summary>
    public string? GetExecutableBinding()
    {
        if (!string.IsNullOrWhiteSpace(KeyboardBinding) && KeyboardBinding.GetInputType() != InputType.MouseAxis)
        {
            return KeyboardBinding;
        }

        if (string.IsNullOrWhiteSpace(MouseBinding))
        {
            return null;
        }

        InputType bindingType = MouseBinding.GetInputType();
        return bindingType is InputType.MouseButton or InputType.MouseWheel ? MouseBinding : null;
    }
}
