using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VInspector;

public class BlockRegistry : MonoBehaviour
{
    public static BlockRegistry Instance;
    public List<BlockDataObject> BlockDataObjects;
    public Dictionary<BlockID, BlockData> Blocks;
    public string BDOFolderPath = "Blocks";

    private void Awake()
    {
        SetInstance();
        PopulateDictionary();
    }

    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public static BlockData LookupBlock(BlockID id)
    {
        Debug.Log($"looking up {id}");
        BlockData result = Instance.Blocks[id];
        return result;
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
    }

}
