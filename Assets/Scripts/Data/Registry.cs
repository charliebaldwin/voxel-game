using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{
    [SerializeField]
    [ShowInInspector]
    public Dictionary<BlockID, BlockData> BlockNames = new Dictionary<BlockID, BlockData>();

    [SerializeField]
    private Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Button]
    public void EnumParseTest()
    {
        BlockID parsedID = (BlockID)Enum.Parse(typeof(BlockID), "Grass");
        Debug.Log(parsedID == BlockID.Grass);
        Debug.Log(parsedID == BlockID.Dirt);
    }
}
