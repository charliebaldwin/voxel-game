using UnityEngine;
using UnityEngine.InputSystem;
using VInspector.Libs;
public enum InputModes
{
    WORLD,
    INVENTORY,
    RADIAL_MENU
}

public class InputHandler : MonoBehaviour
{
    public InputModes CurrentMode;

    public PlayerController playerController;
    public PlayerView playerView;
    public InventoryManager inventory;
    public RadialMenu radialMenu;

    public void OnMove(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                playerController.Move(context.ReadValue<Vector2>());
                break;
        }
    }

    public void OnMouseMove(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                playerController.Aim(context.ReadValue<Vector2>());
                break;
            case InputModes.INVENTORY:
                inventory.SetMousePos(context.ReadValue<Vector2>());
                break;
            case InputModes.RADIAL_MENU:
                radialMenu.SetAngle(context.ReadValue<Vector2>());
                break;
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        int button = context.ReadValue<float>().RoundToInt();

        switch (CurrentMode)
        {
            case InputModes.WORLD:
                switch (button) {
                    case 1:
                        if (context.started)
                            playerView.StartPrimary();
                        else if (context.canceled)
                            playerView.EndPrimary();
                        break;
                    case 2:
                        if (context.started)
                            playerView.StartSecondary();
                        break;
                    case 3: 
                        if (context.started)
                            playerView.StartTertiary();
                        break;
                }
                break;
        }
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                playerView.Scroll(context.ReadValue<float>().RoundToInt());
                break;
            case InputModes.INVENTORY:
                playerView.Scroll(context.ReadValue<float>().RoundToInt());
                break;
        }
    }

    public void OnNumKey(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                playerView.NumKey(context.ReadValue<float>().RoundToInt());
                break;

            case InputModes.INVENTORY:
                playerView.NumKey(context.ReadValue<float>().RoundToInt());
                break;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                playerController.SetSprint(context.ReadValue<float>());
                break;
        }
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        switch (CurrentMode) {
            case InputModes.WORLD:
                if (context.started)
                    playerController.Jump();
                break;
        }
    }



    public void OnInventoryKey(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                Cursor.lockState = CursorLockMode.None;
                CurrentMode = InputModes.INVENTORY;
                inventory.Open();
                break;

            case InputModes.INVENTORY:
                Cursor.lockState = CursorLockMode.Locked;
                CurrentMode = InputModes.WORLD;
                inventory.Close();
                break;
        }
    }

    public void OnRadialKey(InputAction.CallbackContext context)
    {
        switch (CurrentMode)
        {
            case InputModes.WORLD:
                Cursor.lockState = CursorLockMode.None;
                CurrentMode = InputModes.RADIAL_MENU;
                radialMenu.Open();
                break;

            case InputModes.RADIAL_MENU:
                Cursor.lockState = CursorLockMode.Locked;
                CurrentMode = InputModes.WORLD;
                radialMenu.Close();
                break;
        }
    }

}

