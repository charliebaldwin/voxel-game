using System;
using UnityEngine;

[Serializable]
public class BlockData
{
    public BlockID BlockID;
    public int Toughness = 6;
    public bool IsBlockEntity = false;

    public ToolType[] ValidTools;
    public ToolType[] IdealTools;

    public bool CanChangeUpAxis = false;
}



