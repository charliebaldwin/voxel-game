using UnityEngine;
using UnityEngine.InputSystem;
public enum InputModes
{
    WORLD,
    INVENTORY,
    RADIAL_MENU
}

public class InputHandler : MonoBehaviour
{
    public InputModes CurrentMode;
    public PlayerInput Input;

    public PlayerController playerController;
    public PlayerView playerView;
    public InventoryManager inventory;
    public RadialMenu radialMenu;

    public string WorldModeLabel = "World";
    public string GUIModeLabel = "UI";

    public void SetModeWorld(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CurrentMode = InputModes.WORLD;
            Input.SwitchCurrentActionMap(WorldModeLabel);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    public void SetModeWorld()
    {
        CurrentMode = InputModes.WORLD;
        Input.SwitchCurrentActionMap(WorldModeLabel);
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void SetModeInventory(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CurrentMode = InputModes.INVENTORY;
            Input.SwitchCurrentActionMap(GUIModeLabel);
            Cursor.lockState= CursorLockMode.None;
        }
    }
    public void SetModeInventory()
    {
        CurrentMode = InputModes.INVENTORY;
        Input.SwitchCurrentActionMap(GUIModeLabel);
        Cursor.lockState = CursorLockMode.None;
    }
    public void SetModeRadial(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CurrentMode = InputModes.RADIAL_MENU;
            Input.SwitchCurrentActionMap("Radial");
        }
        else if (context.canceled)
        {
            CurrentMode = InputModes.WORLD;
            Input.SwitchCurrentActionMap("World");
        }
    }

    public void OnRadialKey(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CurrentMode = InputModes.RADIAL_MENU;
            Input.SwitchCurrentActionMap("Radial");
        }
        else if (context.canceled)
        {
            CurrentMode = InputModes.WORLD;
            Input.SwitchCurrentActionMap("World");
        }
    }

    public void Test()
    {
        Debug.Log("TEST!!!!");
    }
}

