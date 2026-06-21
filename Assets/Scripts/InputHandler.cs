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

}

