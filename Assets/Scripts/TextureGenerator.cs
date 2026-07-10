using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VInspector;

public class TextureGenerator : MonoBehaviour
{
    public List<Texture2D> Textures;
    public Dictionary<BlockID, Texture2D> BlockTextures;
    public string OutputPath = "Assets/Textures/Generated/Texture_Array.asset";
    public int SliceWidth = 16;
    public int SliceHeight = 16*6;

    [Button]
    public void StitchTextures()
    {
        Texture2DArray array = new Texture2DArray(SliceWidth, SliceHeight, Textures.Count, TextureFormat.RGBA32, true);
        array.filterMode = FilterMode.Point;
        int t = 0;
        foreach (Texture2D tex in Textures)
        {
            for (int m = 0; m < tex.mipmapCount; m++)
            {
                Graphics.CopyTexture(tex, 0, m, array, t, m);
            }
            t++;
        }
        AssetDatabase.CreateAsset(array, OutputPath);
        AssetDatabase.SaveAssets();
    }
}
