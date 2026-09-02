using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Collections.AllocatorManager;

public class BlockRegistry : MonoBehaviour
{
    public static BlockRegistry Instance;
    // objects to populate
    public string BDOFolderPath = "Blocks";
    public static string BDOFolderPathStatic = "BlockData";
    public List<BlockDataObject> BlockDataObjects;
    public List<BlockEntityDataObject> BlockEntityDataObjects;

    public List<BlockData> BlockData;

    // dictionaries
    public Dictionary<BlockID, BlockData> Blocks;
    public Dictionary<BlockID, BlockEntityData> BlockEntities;
    public Dictionary<BlockID, List<int>> BlockTextureIndices;

    public Texture2D DefaultColorTexture;
    public Texture2D DefaultNormalTexture;
    public Texture2D DefaultMSTexture;

    #region INITIALIZE AND LOAD
    private void Awake()
    {
        Debug.Log("Block registry awake");
        SetInstance();
        LoadObjectsFromPath();
        PopulateDictionary();
        ScanTextures();
    }
    [Button]
    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
        Debug.Log("Block registry instance set");

    }

    [Button]
    public void LoadObjectsFromPath()
    {
        BlockDataObject[] bdoArray = Resources.LoadAll<BlockDataObject>(BDOFolderPath);
        BlockDataObjects = new List<BlockDataObject>(bdoArray.Length+2);
        foreach (BlockDataObject bdo in bdoArray)
        {
            BlockDataObjects.Add(bdo);
        }
        //foreach (BlockDataObject bdo in bdoArray)
        //{
        //    Debug.Log($"{bdo.Data.BlockID} = {(int)bdo.Data.BlockID} ({BlockDataObjects.Count})");
        //    BlockDataObjects[(int)bdo.Data.BlockID] = bdo;
            
        //}

        BlockEntityDataObject[] bdoEntityArray = Resources.LoadAll<BlockEntityDataObject>(BDOFolderPath);
        BlockEntityDataObjects = new List<BlockEntityDataObject>(bdoArray.Length);
        int numEntities = 0;
        foreach (BlockEntityDataObject bdo in bdoEntityArray)
        {
            BlockEntityDataObjects.Add(null);
            numEntities++;
        }
        for (int i = 0; i < numEntities; i++)
        {
            BlockEntityDataObjects[i] = bdoEntityArray[i];
        }
    }

    [Button]
    public void PopulateDictionary()
    {
        Blocks = new Dictionary<BlockID, BlockData>();
        foreach (BlockDataObject bdo in BlockDataObjects)
        {
            Debug.Log(bdo.name);
            BlockData data = bdo.Data;
            Blocks.Add(data.BlockID, data);
        }

        BlockEntities = new Dictionary<BlockID, BlockEntityData>();
        foreach(BlockEntityDataObject bdo in BlockEntityDataObjects)
        {
            BlockEntityData data = bdo.Data;
            BlockEntities.Add(data.BlockID, data);
        }
    }

    private List<Texture2D> colorTextures;
    private List<Texture2D> normalTextures;
    private List<Texture2D> msTextures;
    [Button]
    public void ScanTextures()
    {
        BlockTextureIndices = new Dictionary<BlockID, List<int>>();
        colorTextures = new List<Texture2D>();
        normalTextures = new List<Texture2D>();
        msTextures = new List<Texture2D>();

        int index = 0;
        foreach (BlockData block in Blocks.Values)
        {
            if (block.Textures.Count == 0)
            {
                block.Textures.Add(DefaultColorTexture);
            }
            if (block.Textures.Count > 0)
            {
                for (int t=0; t <block.Textures.Count; t++)
                { 
                    colorTextures.Add(block.Textures[t]);
                    if (t < block.NormalTextures.Count)
                    {
                        normalTextures.Add(block.NormalTextures[t]);
                    } else
                    {
                        normalTextures.Add(DefaultNormalTexture);
                    }
                    if (t < block.MSTextures.Count)
                    {
                        msTextures.Add(block.MSTextures[t]);
                    }
                    else
                    {
                        msTextures.Add(DefaultMSTexture);
                    }
                    index++;
                }
                List<int> indices = new List<int>();
                switch (block.TextureMode)
                {
                    case BlockTextureMode.AllFacesSame:
                        for (int i = 0; i < 6; i++)
                        {
                            indices.Add(index-1);
                        }
                        break;

                    case BlockTextureMode.SidesTopBottom:
                        indices.Add(index - 3);
                        indices.Add(index - 3);
                        indices.Add(index - 1);
                        indices.Add(index - 2);
                        indices.Add(index - 3);
                        indices.Add(index - 3);
                        foreach (int d  in indices)
                        {
                            Debug.Log($"{block.BlockID}: index {d}");
                        }
                        break;
                    case BlockTextureMode.SidesAndTop:
                        indices.Add(index - 2);
                        indices.Add(index - 2);
                        indices.Add(index - 1);
                        indices.Add(index - 1);
                        indices.Add(index - 2);
                        indices.Add(index - 2);
                        break;
                    case BlockTextureMode.SixFaces:
                        for (int i = 0; i < 6; i++)
                        {
                            indices.Add(index - 1 - i);
                        }
                        break;

                }
                BlockTextureIndices.Add(block.BlockID, indices);



            }
        }
    }
#endregion


#region REGISTRY LOOKUP

    public static BlockData LookupBlock(BlockID id)
    {
        // Debug.Log($"looking up {id}");
        BlockData result = Instance.Blocks[id];
        return result;
    }
    public static BlockEntityData LookupBlockEntity(BlockID id)
    {
        BlockEntityData result = Instance.BlockEntities[id];
        return result;
    }
    public static List<int> LookupBlockTextures(BlockID id)
    {
        // Debug.Log($"looking up {id}");
        if (Instance.BlockTextureIndices.ContainsKey(id))
        {
            return Instance.BlockTextureIndices[id];
        }
        return new List<int>();
    }

    public static int LookupToughness(BlockID id)
    {
        return LookupBlock(id).Toughness;
    }
    public static bool IsValidTool(BlockID id, ToolType tool)
    {
        return LookupBlock(id).ValidTools.Contains(tool);
    }
    public static bool IsIdealTool(BlockID id, ToolType tool)
    {
        return LookupBlock(id).IdealTools.Contains(tool);
    }

    public List<Texture2D> GetBlockColorTextures()
    {
        return colorTextures;
    }
    public List<Texture2D> GetBlockNormalTextures()
    {
        return normalTextures;
    }
    public List<Texture2D> GetBlockMSTextures()
    {
        return msTextures;
    }



    #endregion


    public static List<BlockData> GetAllBlockData()
    {
        BlockDataObject[] bdoArray = Resources.LoadAll<BlockDataObject>(BDOFolderPathStatic);
        Debug.Log($"BDOs found: {bdoArray.Length}");
        List<BlockData> BlockDataList = new List<BlockData>();
        foreach (BlockDataObject bdo in bdoArray)
        {
            BlockDataList.Add(bdo.Data);
        }
        

        return BlockDataList;
    } 
}
