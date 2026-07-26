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
    public string OutputPathNormal = "Assets/Textures/Generated/Texture_Array_Normal.asset";
    public string OutputPathMS = "Assets/Textures/Generated/Texture_Array_MS.asset";
    public int SliceWidth = 16;
    public int SliceHeight = 16*6;
    public int Columns = 8;
    public Material BlockMaterial;

    public Dictionary<BlockID, List<int>> BlockTextureIndices;

    public Texture2D DefaultColorTexture;
    public Texture2D DefaultNormalTexture;
    public Texture2D DefaultMSTexture;

    [Button]
    public void StitchTextures()
    {
        BlockRegistry.LoadObjectsFromPath();
        BlockRegistry.PopulateDictionary();
        BlockRegistry.ScanTextures();

        List<Texture2D> colorTextures = BlockRegistry.GetBlockColorTextures();
        List<Texture2D> normalTextures = BlockRegistry.GetBlockNormalTextures();
        List<Texture2D> msTextures = BlockRegistry.GetBlockMSTextures();

        // keep this part here
        int numCols = Columns;
        int numRows = colorTextures.Count / Columns + 1;


        // init atlast textures
        Texture2D atlasTextureColor = new Texture2D(16 * numCols, 16 * numRows, TextureFormat.RGBA32, false);
        atlasTextureColor.filterMode = FilterMode.Point;
        atlasTextureColor.FillWithColor(new Color(0f, 0f, 0f, 0f));

        Texture2D atlasTextureNormal = new Texture2D(16 * numCols, 16 * numRows, TextureFormat.RGBA32, false);
        atlasTextureNormal.filterMode = FilterMode.Point;
        atlasTextureNormal.FillWithColor(new Color(0.5f, 0.5f, 1.0f, 0f));

        Texture2D atlasTextureMS = new Texture2D(16 * numCols, 16 * numRows, TextureFormat.RGBA32, false);
        atlasTextureMS.filterMode = FilterMode.Point;
        atlasTextureMS.FillWithColor(new Color(0.0f, 0.2f, 0.0f, 0f));

        // loop through textures
        int row = 0;
        int col = 0;
        for (int i=0; i  < colorTextures.Count; i++)
        {
            
            for (int u=0; u<16;  u++)
            {
                for (int v=0; v<16; v++)
                {
                    Color colorPixel = colorTextures[i].GetPixel(u, v);
                    atlasTextureColor.SetPixel(u + 16 * col, v + 16 * row, colorPixel);

                    Color normalPixel = normalTextures[i].GetPixel(u, v);
                    atlasTextureNormal.SetPixel(u + 16 * col, v + 16 * row, normalPixel);

                    Color msPixel = msTextures[i].GetPixel(u, v);
                    atlasTextureMS.SetPixel(u + 16 * col, v + 16 * row, msPixel);
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
        BlockMaterial.SetTexture("_AtlasTexture", atlasTextureColor);
        BlockMaterial.SetTexture("_AtlasTextureNormal", atlasTextureNormal);
        BlockMaterial.SetTexture("_AtlasTextureMS", atlasTextureMS);
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
        AssetDatabase.CreateAsset(atlasTextureColor, OutputPath);
        AssetDatabase.CreateAsset(atlasTextureNormal, OutputPathNormal);
        AssetDatabase.CreateAsset(atlasTextureMS, OutputPathMS);
        AssetDatabase.SaveAssets();
    }
}
