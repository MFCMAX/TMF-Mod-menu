using Microsoft.Xna.Framework.Input;
using StudioForge.Engine.Core;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;

namespace TMFModMenu.Menu;

internal readonly record struct MenuInputSnapshot(
    bool ControlPressed,
    bool ToggleKeyPressed,
    bool DPadDownPressed,
    bool SelectButtonPressed,
    bool SelectButtonPressedNew,
    bool BackKeyPressed,
    bool ExitPressedNew,
    bool UpPressedNew,
    bool DownPressedNew,
    bool LeftPressedNew,
    bool RightPressedNew,
    bool SelectPressedNew);

internal readonly record struct MenuInputResult(MenuCommand Command, bool IsConsumed);

internal sealed class MenuInput
{
    private bool isPrimed;
    private bool toggleKeyWasPressed;
    private bool backKeyWasHeld;

    public MenuInputResult Poll(ITMPlayer player, bool isOpen)
    {
        var playerIndex = player.PlayerIndex;
        return Map(
            new MenuInputSnapshot(
                InputManager.IsKeyPressed(playerIndex, Keys.LeftControl) ||
                    InputManager.IsKeyPressed(playerIndex, Keys.RightControl),
                InputManager.IsKeyPressed(playerIndex, Keys.L),
                InputManager.IsButtonPressed(playerIndex, Buttons.DPadDown),
                InputManager.IsButtonPressed(playerIndex, Buttons.A),
                InputManager.IsButtonPressedNew(playerIndex, Buttons.A),
                InputManager.IsKeyPressed(playerIndex, Keys.Back),
                InputManager1.IsInputPressedNew(playerIndex, GuiInput.ExitScreen),
                InputManager1.IsInputPressedNew(playerIndex, GuiInput.CursorUp) ||
                    InputManager.IsKeyPressedNew(playerIndex, Keys.Up) ||
                    InputManager.IsButtonPressedNew(playerIndex, Buttons.LeftThumbstickUp),
                InputManager1.IsInputPressedNew(playerIndex, GuiInput.CursorDown) ||
                    InputManager.IsKeyPressedNew(playerIndex, Keys.Down) ||
                    InputManager.IsButtonPressedNew(playerIndex, Buttons.LeftThumbstickDown),
                InputManager1.IsInputPressedNew(playerIndex, GuiInput.CursorLeft) ||
                    InputManager.IsKeyPressedNew(playerIndex, Keys.Left) ||
                    InputManager.IsButtonPressedNew(playerIndex, Buttons.LeftThumbstickLeft),
                InputManager1.IsInputPressedNew(playerIndex, GuiInput.CursorRight) ||
                    InputManager.IsKeyPressedNew(playerIndex, Keys.Right) ||
                    InputManager.IsButtonPressedNew(playerIndex, Buttons.LeftThumbstickRight),
                    InputManager.IsButtonPressedNew(playerIndex, Buttons.A)),
            isOpen);
    }

    internal MenuInputResult Map(MenuInputSnapshot input, bool isOpen)
    {
        var controllerChordHeld =
            input.DPadDownPressed && input.SelectButtonPressed;
        var keyboardChordHeld = input.ControlPressed && input.ToggleKeyPressed;
        if (!isPrimed)
        {
            isPrimed = true;
            toggleKeyWasPressed = input.ToggleKeyPressed;
            backKeyWasHeld = input.BackKeyPressed;
            return new MenuInputResult(
                MenuCommand.None,
                isOpen || keyboardChordHeld || controllerChordHeld);
        }

        // L must rise while Control is already held. This prevents an L-first
        // sequence from rebuilding local light and then opening the menu.
        var keyboardChordPressedNew =
            keyboardChordHeld && !toggleKeyWasPressed;
        // Match the host tutorial's safe order: DPad Down held, then A new.
        // Accepting A first would let Jump fire before the menu opens.
        var controllerChordPressedNew =
            controllerChordHeld && input.SelectButtonPressedNew;
        var backKeyPressedNew = input.BackKeyPressed && !backKeyWasHeld;

        toggleKeyWasPressed = input.ToggleKeyPressed;
        backKeyWasHeld = input.BackKeyPressed;

        if (keyboardChordPressedNew || controllerChordPressedNew)
            return new MenuInputResult(MenuCommand.Toggle, true);

        // Suppress the controller chord until either button is released so it
        // cannot bleed into normal gameplay or menu navigation.
        if (keyboardChordHeld || controllerChordHeld)
            return new MenuInputResult(MenuCommand.None, true);

        if (isOpen && (backKeyPressedNew || input.ExitPressedNew))
            return new MenuInputResult(MenuCommand.Back, true);

        if (isOpen && input.UpPressedNew)
            return new MenuInputResult(MenuCommand.Up, true);

        if (isOpen && input.DownPressedNew)
            return new MenuInputResult(MenuCommand.Down, true);

        if (isOpen && input.LeftPressedNew)
            return new MenuInputResult(MenuCommand.Left, true);

        if (isOpen && input.RightPressedNew)
            return new MenuInputResult(MenuCommand.Right, true);

        if (isOpen && input.SelectPressedNew)
            return new MenuInputResult(MenuCommand.Select, true);

        if (isOpen)
            return new MenuInputResult(MenuCommand.None, true);

        return new MenuInputResult(MenuCommand.None, false);
    }

    public void Reset()
    {
        isPrimed = false;
        toggleKeyWasPressed = false;
        backKeyWasHeld = false;
    }
}
