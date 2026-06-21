using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RadialMenu : MonoBehaviour
{
    public Image UIImage;
    public Material UIMaterial;
    public CanvasGroup CanvasGroup;


    public void Open()
    {
        CanvasGroup.alpha = 1f;
    }
    public void Close()
    {
        CanvasGroup.alpha = 0f;
    }

    public void OnRadialKey (InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            Close();
        }
    }

    public void OnMousePos(InputAction.CallbackContext context)
    {
        Vector2 mouseScreenPos = context.ReadValue<Vector2>();
        Vector2 uv = new Vector2(mouseScreenPos.x / Screen.height - 0.4f, mouseScreenPos.y / Screen.height);
        uv -= new Vector2(0.5f, 0.5f);
        float radius = uv.magnitude;

        float angle = Mathf.Rad2Deg * Mathf.Atan2(uv.x, uv.y) + 180f;
        UIMaterial.SetFloat("_Rotation", angle);
        UIMaterial.SetFloat("_Radius", radius);
    }

    public void SetAngle(Vector2 mouseScreenPos)
    {
        Vector2 uv = new Vector2(mouseScreenPos.x / Screen.height - 0.4f, mouseScreenPos.y / Screen.height );
        uv -= new Vector2(0.5f, 0.5f);
        float radius = uv.magnitude;

        float angle = Mathf.Rad2Deg * Mathf.Atan2(uv.x, uv.y) + 180f;
        UIMaterial.SetFloat("_Rotation", angle);
        UIMaterial.SetFloat("_Radius", radius);

    }

    public void Start()
    {
    }
}
