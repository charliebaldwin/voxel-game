using UnityEngine;
using UnityEngine.InputSystem;

public class GUIController : MonoBehaviour
{
    public InventoryManager Inventory;
    public CanvasGroup Popup;
    public InputHandler InputHandler;

    private Vector3 mousePos = Vector3.zero;

    #region INPUT HANDLERS

    public void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Inventory.Open();
            InputHandler.SetModeInventory();
        }
    }
    public void OnExit(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Inventory.IsOpen)
            {
                Inventory.Close();
            }
            if (Popup.alpha > 0f)
            {
                Popup.alpha = 0f;
            }
            InputHandler.SetModeWorld();
        }
    }

    public void OnMoveMouse(InputAction.CallbackContext context)
    { 
        Vector2 pos2D = context.ReadValue<Vector2>();
        mousePos = new Vector3(pos2D.x, pos2D.y, -5f);
        
        if (Inventory.IsOpen)
        {
            Inventory.SetMousePos(mousePos);
        }

    }

    public void OpenPopup()
    {
        Popup.alpha = 1f;
        InputHandler.SetModeInventory();
        Mouse.current.WarpCursorPosition(new Vector2(mousePos.x, mousePos.y));
    }

    #endregion
}
