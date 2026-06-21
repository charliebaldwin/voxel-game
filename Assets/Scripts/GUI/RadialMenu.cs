using UnityEngine;
using UnityEngine.UI;

public class RadialMenu : MonoBehaviour
{
    public Image UIImage;
    public Material UIMaterial;


    public void Open()
    {
        gameObject.SetActive(true);
    }
    public void Close()
    {
        gameObject.SetActive(false);
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
