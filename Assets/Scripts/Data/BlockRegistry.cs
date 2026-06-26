using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VInspector;

public class BlockRegistry : MonoBehaviour
{
    public static BlockRegistry Instance;
    // objects to populate
    public string BDOFolderPath = "Blocks";
    public List<BlockDataObject> BlockDataObjects;
    public List<BlockEntityDataObject> BlockEntityDataObjects;

    // dictionaries
    public Dictionary<BlockID, BlockData> Blocks;
    public Dictionary<BlockID, BlockEntityData> BlockEntities;


#region INITIALIZE AND LOAD
    private void Awake()
    {
        SetInstance();
        LoadObjectsFromPath();
        PopulateDictionary();
    }
    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    [Button]
    private void LoadObjectsFromPath()
    {
        BlockDataObject[] bdoArray = Resources.LoadAll<BlockDataObject>(BDOFolderPath);
        BlockDataObjects = new List<BlockDataObject>(bdoArray.Length);
        foreach (BlockDataObject bdo in bdoArray)
        {
            BlockDataObjects.Add(null);
        }
        foreach (BlockDataObject bdo in bdoArray)
        {
            BlockDataObjects[(int)bdo.Data.BlockID] = bdo;
            //Debug.Log($"{bdo.Data.ItemID} - {bdo.Data.Name}");
        }

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
    private void PopulateDictionary()
    {
        Blocks = new Dictionary<BlockID, BlockData>();
        foreach (BlockDataObject bdo in BlockDataObjects)
        {
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

#endregion
}
