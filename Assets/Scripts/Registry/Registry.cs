using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VFolders.Libs;

[CreateAssetMenu(fileName = "Registry", menuName = "Scriptable Objects/Registry")]

public class Registry : SerializedScriptableObject
{
    public const int ATLAS_COLUMNS = 8;
    public const int CELL_SIZE = 16;

    [SerializeField] private List<ItemType> Items;
    [SerializeField] private List<BlockType> Blocks;

    private static Dictionary<string, BlockType> m_blocksByIDString;
    private static BlockType[] m_blocksByIndex;


    public string MainTextureAtlasPath = "Assets/Textures/Generated/Atlas_Main.asset";
    public List<string> TextureFeatureAtlasPaths = new List<string>
    {
        "Assets/Textures/Generated/Atlas_Feature_Grass.asset"
    };


    public static Registry Instance;
    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }


    [Button]
    private void RegisterBlocks()
    {
        List<Texture2D> mainTextures = GatherTextures();
        GenerateTexture(mainTextures, MainTextureAtlasPath);

        for (int f = 0; f < TextureFeatureAtlasPaths.Count; f++)
        {
            List<Texture2D> featureTextures = GatherFeatureTextures(f);
            GenerateTexture(featureTextures, TextureFeatureAtlasPaths[f]);
        }



        m_blocksByIDString = new Dictionary<string, BlockType>();
        m_blocksByIndex = new BlockType[Blocks.Count];

        int i = 0;
        foreach (BlockType b in Blocks)
        {
            if (b.ID == null || b.ID == "null" || b.ID.Length == 0)
            {
                Debug.LogError($"REGISTRY ERROR: Block type at index {i} has invalid ID string!");
            }
            else
            {
                if (m_blocksByIDString.ContainsKey(b.ID)) { Debug.LogError($"REGISTRY ERROR: Registry already has block with ID string {b.ID}!"); }
                else
                {
                    m_blocksByIDString.Add(b.ID, b);
                    m_blocksByIndex[i] = b;
                }
                i++;
            }
        }


    }

    private List<Texture2D> GatherTextures()
    {
        List<Texture2D> result = new List<Texture2D>();
        int index = 0;
        foreach (BlockType b in Blocks)
        {
            if (b.TextureGroup == null) { Debug.LogError($"REGISTRY ERROR: block with ID {b.ID} has no BlockTextureGroup!"); }
            else if (b.TextureGroup.Textures == null || b.TextureGroup.Textures.Count == 0) { Debug.LogError($"REGISTRY ERROR: block with ID {b.ID} has BlockTextureGroup with empty/invalid texture list!"); }
            else
            {
                b.TextureGroup.TextureIndices = new List<int>();
                foreach (Texture2D t in b.TextureGroup.Textures)
                {
                    result.Add(t);
                    b.TextureGroup.TextureIndices.Add(index);
                    index++;
                }
            }
        }
        return result;
    }
    private List<Texture2D> GatherFeatureTextures(int featureIndex)
    {
        List<Texture2D> result = new List<Texture2D>();
        int index = 0;
        foreach (BlockType b in Blocks)
        {
            if (b.TextureFeatures == null) { continue; }
            if (b.TextureFeatures.Count < featureIndex + 1) { continue; }
            if (b.TextureFeatures[featureIndex] == null) { continue; }

            b.TextureFeatures[featureIndex].TextureIndices = new List<int>();
            foreach (Texture2D t in b.TextureFeatures[featureIndex].GetTextures())
            {
                result.Add(t);
                b.TextureFeatures[featureIndex].TextureIndices.Add(index);
                index++;
            }
        }
        return result;
    }

    private void GenerateTexture(List<Texture2D> textures, string outputPath)
    {
        int cols = Math.Min(ATLAS_COLUMNS, textures.Count);
        int rows = textures.Count / cols + 1;
        Texture2D atlas = new Texture2D(CELL_SIZE * cols, CELL_SIZE * rows, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;
        atlas.FillWithColor(Color.clear);

        int row = 0;
        int col = 0;

        for (int i = 0; i < textures.Count; i++)
        {
            for (int u = 0; u < 16; u++)
            {
                for (int v = 0; v < 16; v++)
                {
                    Color colorPixel = textures[i].GetPixel(u, v);
                    atlas.SetPixel(u + 16 * col, v + 16 * row, colorPixel);
                }
            }
            col++;
            if (col >= cols)
            {
                col = 0;
                row++;
            }
        }
        AssetDatabase.CreateAsset(atlas, outputPath);
        AssetDatabase.SaveAssets();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


}


