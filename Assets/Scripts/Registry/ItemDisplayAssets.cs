using UnityEditor;
using UnityEngine;

public class ItemDisplayAssets
{
    public Sprite GUISprite;
    public Mesh ViewmodelMesh;

    public ItemDisplayAssets()
    {
        GUISprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Icons/Icon_Default.png");
    }
}