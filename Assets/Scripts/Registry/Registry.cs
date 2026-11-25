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
    public bool RefreshOnValidate = true;
    public const int ATLAS_COLUMNS = 4;
    private int rows, cols;
    public const int CELL_SIZE = 16;

    [SerializeField] private List<ItemType> Items;
    [SerializeField] private List<BlockType> Blocks;

    private static Dictionary<string, BlockType> m_blocksByIDString;
    private static BlockType[] m_blocksByIndex;
    private static string[] m_blockIDs;


    public string MainTextureAtlasPath = "Assets/Textures/Generated/Atlas_Main.asset";
    public List<string> TextureFeatureAtlasPaths = new List<string>
    {
        "Assets/Textures/Generated/Atlas_Feature_Grass.asset"
    };
    public Material TargetMaterial;


    public void Initialize()
    {
        SetInstance();
    }
    public static Registry Instance;
    //[Button]
    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }
    private void OnValidate()
    {
        if (RefreshOnValidate) RegisterBlocks();
    }



    [Button]
    private void RegisterBlocks()
    {
        SetInstance();

        List<Texture2D> mainTextures = GatherTextures();
        Texture2D mainTex = GenerateTexture(mainTextures, MainTextureAtlasPath);
        TargetMaterial.SetTexture("_TextureAtlas", mainTex);
        TargetMaterial.SetInt("_Rows", rows);
        TargetMaterial.SetInt("_Cols", cols);
        for (int f = 0; f < TextureFeatureAtlasPaths.Count; f++)
        {
            List<Texture2D> featureTextures = GatherFeatureTextures(f);
            Texture2D featureTex = GenerateTexture(featureTextures, TextureFeatureAtlasPaths[f]);
            TargetMaterial.SetTexture($"_FeatureAtlas{f}", featureTex);
        }



        m_blocksByIDString = new Dictionary<string, BlockType>();
        m_blocksByIndex = new BlockType[Blocks.Count];
        m_blockIDs = new string[Blocks.Count];

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
                    m_blockIDs[i] = b.ID;
                }
                i++;
            }
        }


    }

    private List<Texture2D> GatherTextures()
    {
        List<Texture2D> result = new List<Texture2D>();
        int index = 0;
        for (int i = 0; i < Blocks.Count; i++)
        {
            BlockType b = Blocks[i];
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

    private Texture2D GenerateTexture(List<Texture2D> textures, string outputPath)
    {
        cols = Math.Min(ATLAS_COLUMNS, textures.Count);
        rows = textures.Count / cols + 1;

        Texture2D atlas = new Texture2D(CELL_SIZE * cols, CELL_SIZE * rows, TextureFormat.RGBA32, true);
        atlas.filterMode = FilterMode.Point;
        atlas.FillWithColor(Color.clear);

        int row = 0;
        int col = 0;

        for (int i = 0; i < textures.Count; i++)
        {
            for (int u = 0; u < CELL_SIZE; u++)
            {
                for (int v = 0; v < CELL_SIZE; v++)
                {
                    Color colorPixel = textures[i].GetPixel(u, v);
                    atlas.SetPixel(u + CELL_SIZE * col, v + CELL_SIZE * row, colorPixel);
                }
            }
            col++;
            if (col >= cols)
            {
                col = 0;
                row++;
            }
        }
        atlas.Apply(updateMipmaps: true);
        AssetDatabase.CreateAsset(atlas, outputPath);
        AssetDatabase.SaveAssets();
        return atlas;
    }

    public static int GetBlockID(string type)
    {
        if (m_blocksByIDString.ContainsKey(type))
        {
            return Array.IndexOf(m_blockIDs, type);
        }
        else
        {
            Debug.LogError($"REGISTRY ERROR: tried looking up block type ({type}), which was not present in list!");
            return -1;
        }
    }
    public static BlockType GetBlockType(Voxel vox)
    {
        if (m_blocksByIndex.Length > vox.BlockType)
        {
            return m_blocksByIndex[vox.BlockType];
        }
        else return null;
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


