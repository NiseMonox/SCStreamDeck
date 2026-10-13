using FluentAssertions;
using Moq;
using SCStreamDeck.Common;
using SCStreamDeck.Models;
using SCStreamDeck.Services.Keybinding;
using WindowsInput;
using WindowsInput.Native;

namespace Tests.Unit.Services.Keybinding;

public sealed class KeybindingExecutorServiceTests
{
    private static KeybindingLoaderService CreateLoader(ActivationMode mode, ActivationModeMetadata metadata)
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());
        loader.LoadKeybindings(new KeybindingDataFile
        {
            Metadata = new KeybindingMetadata
            {
                ActivationModes = new Dictionary<string, ActivationModeMetadata> { [mode.ToString()] = metadata }
            },
            Actions =
            [
                new KeybindingActionData
                {
                    Name = "test_action",
                    MapName = "test_map",
                    Label = "Test",
                    Category = "Test",
                    ActivationMode = mode,
                    Bindings = new InputBindings { Keyboard = "f1" }
                }
            ]
        }).Should().BeTrue();

        return loader;
    }

    [Fact]
    public async Task ExecuteAsync_HoldMode_PressesBeforeReleasing_WhenDownAndUpArriveBackToBack()
    {
        List<string> order = [];

        Mock<IKeyboardSimulator> keyboard = new(MockBehavior.Strict);
        keyboard.Setup(k => k.KeyDown(DirectInputKeyCode.DikF1))
            .Callback(() =>
            {
                Thread.Sleep(50); // give the release every chance to overtake the press
                lock (order)
                {
                    order.Add("down");
                }
            })
            .Returns(keyboard.Object);
        keyboard.Setup(k => k.KeyUp(DirectInputKeyCode.DikF1))
            .Callback(() =>
            {
                lock (order)
                {
                    order.Add("up");
                }
            })
            .Returns(keyboard.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Keyboard).Returns(keyboard.Object);

        KeybindingExecutorService service = new(new KeybindingLoaderService(new SystemFileSystem()), inputSimulator.Object);

        KeybindingExecutionContext Context(bool isKeyDown) => new()
        {
            ActionName = "test_action",
            Binding = "f1",
            ActivationMode = ActivationMode.hold,
            IsKeyDown = isKeyDown
        };

        Task<bool> down = service.ExecuteAsync(Context(true));
        Task<bool> up = service.ExecuteAsync(Context(false));
        await Task.WhenAll(down, up);

        order.Should().Equal("down", "up");
    }

    [Fact]
    public async Task ExecuteOneShotAsync_DoubleTapAction_TapsTwice()
    {
        KeybindingLoaderService loader = CreateLoader(ActivationMode.double_tap,
            new ActivationModeMetadata { OnPress = true, MultiTap = 2, MultiTapBlock = 1 });

        Mock<IKeyboardSimulator> keyboard = new(MockBehavior.Strict);
        keyboard.Setup(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50)).Returns(keyboard.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Keyboard).Returns(keyboard.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecuteOneShotAsync("test_action", "f1");

        result.Should().BeTrue();
        keyboard.Verify(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteOneShotAsync_LongPressAction_HoldsTheKeyPastItsThreshold()
    {
        KeybindingLoaderService loader = CreateLoader(ActivationMode.delayed_press,
            new ActivationModeMetadata { OnPress = true, MultiTap = 1, PressTriggerThreshold = 0.1f });

        long downAt = 0;
        long upAt = 0;
        Mock<IKeyboardSimulator> keyboard = new(MockBehavior.Strict);
        keyboard.Setup(k => k.KeyDown(DirectInputKeyCode.DikF1))
            .Callback(() => downAt = Environment.TickCount64)
            .Returns(keyboard.Object);
        keyboard.Setup(k => k.KeyUp(DirectInputKeyCode.DikF1))
            .Callback(() => upAt = Environment.TickCount64)
            .Returns(keyboard.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Keyboard).Returns(keyboard.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecuteOneShotAsync("test_action", "f1");

        result.Should().BeTrue();
        keyboard.Verify(k => k.KeyDown(DirectInputKeyCode.DikF1), Times.Once);
        keyboard.Verify(k => k.KeyUp(DirectInputKeyCode.DikF1), Times.Once);
        (upAt - downAt).Should().BeGreaterThanOrEqualTo(150, "the key must stay down for the 0.1 s threshold plus a margin");
    }

    [Fact]
    public async Task ExecuteOneShotAsync_PressAction_PressesOnce()
    {
        KeybindingLoaderService loader = CreateLoader(ActivationMode.press,
            new ActivationModeMetadata { OnPress = true, MultiTap = 1, PressTriggerThreshold = -1f });

        Mock<IKeyboardSimulator> keyboard = new(MockBehavior.Strict);
        keyboard.Setup(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50)).Returns(keyboard.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Keyboard).Returns(keyboard.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecuteOneShotAsync("test_action", "f1");

        result.Should().BeTrue();
        keyboard.Verify(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50), Times.Once);
    }

    [Fact]
    public async Task ExecutePressNoRepeatAsync_KeyboardSingleKey_ExecutesDelayedKeyPress()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Mock<IKeyboardSimulator> keyboard = new(MockBehavior.Strict);
        keyboard.Setup(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50)).Returns(keyboard.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Keyboard).Returns(keyboard.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecutePressNoRepeatAsync("TestAction", "f1");

        result.Should().BeTrue();
        keyboard.Verify(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50), Times.Once);
    }

    [Fact]
    public async Task ExecutePressNoRepeatAsync_MouseLeftButton_ExecutesLeftClick()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Mock<IMouseSimulator> mouse = new(MockBehavior.Strict);
        mouse.Setup(m => m.LeftButtonClick()).Returns(mouse.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Mouse).Returns(mouse.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecutePressNoRepeatAsync("TestAction", SCConstants.Input.Mouse.LeftButton);

        result.Should().BeTrue();
        mouse.Verify(m => m.LeftButtonClick(), Times.Once);
    }

    [Fact]
    public async Task ExecutePressNoRepeatAsync_MouseWheelUp_ExecutesVerticalScroll()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Mock<IMouseSimulator> mouse = new(MockBehavior.Strict);
        mouse.Setup(m => m.VerticalScroll(1)).Returns(mouse.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Mouse).Returns(mouse.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecutePressNoRepeatAsync("TestAction", SCConstants.Input.Mouse.WheelUp);

        result.Should().BeTrue();
        mouse.Verify(m => m.VerticalScroll(1), Times.Once);
    }

    [Fact]
    public async Task ExecutePressNoRepeatAsync_MouseWheelDown_ExecutesVerticalScroll()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Mock<IMouseSimulator> mouse = new(MockBehavior.Strict);
        mouse.Setup(m => m.VerticalScroll(-1)).Returns(mouse.Object);

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);
        inputSimulator.SetupGet(i => i.Mouse).Returns(mouse.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecutePressNoRepeatAsync("TestAction", SCConstants.Input.Mouse.WheelDown);

        result.Should().BeTrue();
        mouse.Verify(m => m.VerticalScroll(-1), Times.Once);
    }

    [Fact]
    public async Task ExecutePressNoRepeatAsync_InvalidBinding_ReturnsFalse_AndDoesNotExecute()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Mock<IInputSimulator> inputSimulator = new(MockBehavior.Strict);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        bool result = await service.ExecutePressNoRepeatAsync("TestAction", "$$invalid$$");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenContextInvalid()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());
        KeybindingExecutorService service = new(loader, Mock.Of<IInputSimulator>());

        KeybindingExecutionContext context = new()
        {
            ActionName = string.Empty,
            Binding = string.Empty,
            ActivationMode = ActivationMode.press,
            IsKeyDown = true
        };

        bool result = await service.ExecuteAsync(context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenParseFails()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());
        KeybindingExecutorService service = new(loader, Mock.Of<IInputSimulator>());

        KeybindingExecutionContext context = new()
        {
            ActionName = "TestAction",
            Binding = "$$invalid$$",
            ActivationMode = ActivationMode.press,
            IsKeyDown = true
        };

        bool result = await service.ExecuteAsync(context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesPressUsingMetadata()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Mock<IKeyboardSimulator> keyboard = new();
        Mock<IInputSimulator> inputSimulator = new();
        inputSimulator.Setup(i => i.Keyboard).Returns(keyboard.Object);

        KeybindingExecutorService service = new(loader, inputSimulator.Object);

        KeybindingExecutionContext context = new()
        {
            ActionName = "TestAction",
            Binding = "F1",
            ActivationMode = ActivationMode.press,
            IsKeyDown = true
        };

        bool result = await service.ExecuteAsync(context);

        result.Should().BeTrue();
        keyboard.Verify(k => k.DelayedKeyPress(DirectInputKeyCode.DikF1, 50), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoaderServiceIsNull()
    {
        Action act = () => new KeybindingExecutorService(null!, Mock.Of<IInputSimulator>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("loaderService");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenInputSimulatorIsNull()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());

        Action act = () => new KeybindingExecutorService(loader, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inputSimulator");
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());
        KeybindingExecutorService service = new(loader, Mock.Of<IInputSimulator>());

        Func<Task> act = async () => await service.ExecuteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        KeybindingLoaderService loader = new(new SystemFileSystem());
        KeybindingExecutorService service = new(loader, Mock.Of<IInputSimulator>());

        Action act = () =>
        {
            service.Dispose();
            service.Dispose();
        };

        act.Should().NotThrow();
    }
}
