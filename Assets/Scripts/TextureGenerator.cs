using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using VFolders.Libs;

public class TextureGenerator : MonoBehaviour
{
    public BlockRegistry BlockRegistry;
    public List<Texture2D> Textures;
    public Dictionary<BlockID, Texture2D> BlockTextures;
    public string OutputPath = "Assets/Textures/Generated/Texture_Array.asset";
    public int SliceWidth = 16;
    public int SliceHeight = 16*6;
    public int Columns = 8;
    public Material BlockMaterial;

    public Dictionary<BlockID, List<int>> BlockTextureIndices;

    [Button]
    public void StitchTextures()
    {
        BlockRegistry.LoadObjectsFromPath();
        BlockRegistry.PopulateDictionary();
        BlockRegistry.ScanTextures();

        List<Texture2D> textures = BlockRegistry.GetBlockTextures();
        Debug.Log($"num textures = {textures.Count}");

        // keep this part here
        int numCols = Columns;
        int numRows = textures.Count / Columns + 1;


        Texture2D atlasTexture = new Texture2D(16 * numCols, 16 * numRows, TextureFormat.RGBA32, true);
        atlasTexture.filterMode = FilterMode.Point;
        atlasTexture.FillWithColor(new Color(0f, 0f, 0f, 0f));
        int row = 0;
        int col = 0;
        for (int i=0; i <textures.Count; i++)
        {
            for (int u=0; u<16;  u++)
            {
                for (int v=0; v<16; v++)
                {
                    Color pixel1 = textures[i].GetPixel(u, v);
                    atlasTexture.SetPixel(u + 16 * col, v + 16 * row, pixel1);
                }
            }
            col++;
            if (col >= Columns)
            {
                col = 0;
                row++;
            }
        }

        BlockMaterial.SetInt("_Rows", numRows);
        BlockMaterial.SetInt("_Columns", numCols);
        BlockMaterial.SetTexture("_AtlasTexture", atlasTexture);
        //Texture2DArray array = new Texture2DArray(SliceWidth, SliceHeight, Textures.Count, TextureFormat.RGBA32, true);
        //array.filterMode = FilterMode.Point;
        //int t = 0;
        //foreach (Texture2D tex in Textures)
        //{
        //    for (int m = 0; m < tex.mipmapCount; m++)
        //    {
        //        Graphics.CopyTexture(tex, 0, m, array, t, m);
        //    }
        //    t++;
        //}
        AssetDatabase.CreateAsset(atlasTexture, OutputPath);
        AssetDatabase.SaveAssets();
    }
}
