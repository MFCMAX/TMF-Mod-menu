using TMFModMenu.Menu;

namespace TMFModMenu.Tests;

public sealed class MenuInputTests
{
    [Fact]
    public void NewControlLPressTogglesOnceAndHeldFrameDoesNothing()
    {
        var input = new MenuInput();
        Prime(input);
        var firstFrame = input.Map(
            Snapshot(controlPressed: true, toggleKeyPressed: true),
            isOpen: false);
        var heldFrame = input.Map(
            Snapshot(controlPressed: true, toggleKeyPressed: true),
            isOpen: true);
        input.Map(Snapshot(), isOpen: true);
        var secondPress = input.Map(
            Snapshot(controlPressed: true, toggleKeyPressed: true),
            isOpen: true);

        Assert.Equal(MenuCommand.Toggle, firstFrame.Command);
        Assert.True(firstFrame.IsConsumed);
        Assert.Equal(MenuCommand.None, heldFrame.Command);
        Assert.True(heldFrame.IsConsumed);
        Assert.Equal(MenuCommand.Toggle, secondPress.Command);
        Assert.True(secondPress.IsConsumed);
    }

    [Fact]
    public void DPadDownThenAChordTogglesAndIsConsumedUntilReleased()
    {
        var input = new MenuInput();
        Prime(input);
        input.Map(Snapshot(dPadDownPressed: true), isOpen: false);
        var press = input.Map(
            Snapshot(
                dPadDownPressed: true,
                selectButtonPressed: true,
                selectButtonPressedNew: true),
            isOpen: false);
        var held = input.Map(
            Snapshot(dPadDownPressed: true, selectButtonPressed: true),
            isOpen: true);
        var released = input.Map(Snapshot(), isOpen: true);

        Assert.Equal(MenuCommand.Toggle, press.Command);
        Assert.True(press.IsConsumed);
        Assert.Equal(MenuCommand.None, held.Command);
        Assert.True(held.IsConsumed);
        Assert.Equal(MenuCommand.None, released.Command);
        Assert.True(released.IsConsumed);
    }

    [Fact]
    public void AFirstThenDPadDoesNotOpenAfterJumpCouldHaveFired()
    {
        var input = new MenuInput();
        Prime(input);
        input.Map(
            Snapshot(selectButtonPressed: true, selectButtonPressedNew: true),
            isOpen: false);

        var result = input.Map(
            Snapshot(dPadDownPressed: true, selectButtonPressed: true),
            isOpen: false);

        Assert.Equal(MenuCommand.None, result.Command);
        Assert.True(result.IsConsumed);
    }

    [Fact]
    public void ExitClosesOnlyWhenMenuIsOpen()
    {
        var input = new MenuInput();
        Prime(input);
        var closed = input.Map(Snapshot(exitPressedNew: true), isOpen: false);
        var open = input.Map(Snapshot(exitPressedNew: true), isOpen: true);

        Assert.Equal(MenuCommand.None, closed.Command);
        Assert.False(closed.IsConsumed);
        Assert.Equal(MenuCommand.Back, open.Command);
        Assert.True(open.IsConsumed);
    }

    [Fact]
    public void ClosedMenuLeavesUnrelatedInputUnhandled()
    {
        var input = new MenuInput();
        Prime(input);

        var result = input.Map(Snapshot(), isOpen: false);

        Assert.Equal(MenuCommand.None, result.Command);
        Assert.False(result.IsConsumed);
    }

    [Fact]
    public void PlainLIsLeftToTheVanillaLightingBinding()
    {
        var input = new MenuInput();
        Prime(input);

        var result = input.Map(
            Snapshot(toggleKeyPressed: true),
            isOpen: false);

        Assert.Equal(MenuCommand.None, result.Command);
        Assert.False(result.IsConsumed);
    }

    [Fact]
    public void BackspaceClosesOnceAndHeldFrameDoesNothing()
    {
        var input = new MenuInput();
        Prime(input);
        var firstFrame = input.Map(Snapshot(backKeyPressed: true), isOpen: true);
        var heldFrame = input.Map(Snapshot(backKeyPressed: true), isOpen: false);

        Assert.Equal(MenuCommand.Back, firstFrame.Command);
        Assert.True(firstFrame.IsConsumed);
        Assert.Equal(MenuCommand.None, heldFrame.Command);
        Assert.False(heldFrame.IsConsumed);
    }

    [Fact]
    public void LFirstThenControlDoesNotOpenTheMenu()
    {
        var input = new MenuInput();
        Prime(input);
        input.Map(Snapshot(toggleKeyPressed: true), isOpen: false);

        var result = input.Map(
            Snapshot(controlPressed: true, toggleKeyPressed: true),
            isOpen: false);

        Assert.Equal(MenuCommand.None, result.Command);
        Assert.True(result.IsConsumed);
    }

    [Fact]
    public void ResetWhileChordIsHeldRequiresAReleaseBeforeOpening()
    {
        var input = new MenuInput();
        Prime(input);
        input.Reset();

        var heldAfterReset = input.Map(
            Snapshot(controlPressed: true, toggleKeyPressed: true),
            isOpen: false);
        input.Map(Snapshot(), isOpen: false);
        var newPress = input.Map(
            Snapshot(controlPressed: true, toggleKeyPressed: true),
            isOpen: false);

        Assert.Equal(MenuCommand.None, heldAfterReset.Command);
        Assert.True(heldAfterReset.IsConsumed);
        Assert.Equal(MenuCommand.Toggle, newPress.Command);
    }

    [Fact]
    public void OpenMenuMapsOneNavigationEdgeAndIgnoresHeldFrame()
    {
        var input = new MenuInput();
        Prime(input);

        var firstFrame = input.Map(Snapshot(downPressedNew: true), isOpen: true);
        var heldFrame = input.Map(Snapshot(), isOpen: true);

        Assert.Equal(MenuCommand.Down, firstFrame.Command);
        Assert.True(firstFrame.IsConsumed);
        Assert.Equal(MenuCommand.None, heldFrame.Command);
        Assert.True(heldFrame.IsConsumed);
    }

    [Fact]
    public void ClosedMenuDoesNotConsumeNavigationEdges()
    {
        var input = new MenuInput();
        Prime(input);

        var up = input.Map(Snapshot(upPressedNew: true), isOpen: false);
        var down = input.Map(Snapshot(downPressedNew: true), isOpen: false);

        Assert.Equal(MenuCommand.None, up.Command);
        Assert.False(up.IsConsumed);
        Assert.Equal(MenuCommand.None, down.Command);
        Assert.False(down.IsConsumed);
    }

    [Fact]
    public void ControllerSelectFiresOnceAndHeldFrameDoesNothing()
    {
        var input = new MenuInput();
        Prime(input);

        var press = input.Map(Snapshot(selectPressedNew: true), isOpen: true);
        var held = input.Map(Snapshot(), isOpen: true);

        Assert.Equal(MenuCommand.Select, press.Command);
        Assert.True(press.IsConsumed);
        Assert.Equal(MenuCommand.None, held.Command);
        Assert.True(held.IsConsumed);
    }

    [Theory]
    [InlineData(true, false, 6)]
    [InlineData(false, true, 7)]
    public void HorizontalArrowMapsToOneCommand(
        bool left,
        bool right,
        int expected)
    {
        var input = new MenuInput();
        Prime(input);

        var press = input.Map(
            Snapshot(leftPressedNew: left, rightPressedNew: right),
            isOpen: true);
        var held = input.Map(Snapshot(), isOpen: true);

        Assert.Equal((MenuCommand)expected, press.Command);
        Assert.True(press.IsConsumed);
        Assert.Equal(MenuCommand.None, held.Command);
    }

    private static MenuInputSnapshot Snapshot(
        bool controlPressed = false,
        bool toggleKeyPressed = false,
        bool dPadDownPressed = false,
        bool selectButtonPressed = false,
        bool selectButtonPressedNew = false,
        bool backKeyPressed = false,
        bool exitPressedNew = false,
        bool upPressedNew = false,
        bool downPressedNew = false,
        bool leftPressedNew = false,
        bool rightPressedNew = false,
        bool selectPressedNew = false) =>
        new(
            controlPressed,
            toggleKeyPressed,
            dPadDownPressed,
            selectButtonPressed,
            selectButtonPressedNew,
            backKeyPressed,
            exitPressedNew,
            upPressedNew,
            downPressedNew,
            leftPressedNew,
            rightPressedNew,
            selectPressedNew);

    private static void Prime(MenuInput input) =>
        input.Map(Snapshot(), isOpen: false);
}
