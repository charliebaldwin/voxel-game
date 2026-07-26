using NUnit.Framework;
using System;
using System.Collections.Generic;
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

    public Material Material;

    public List<Texture2D> Textures;
    public List<Texture2D> NormalTextures;
    public List<Texture2D> MSTextures;
    public BlockTextureMode TextureMode;
}

public enum BlockTextureMode
{
    AllFacesSame,
    SidesAndTop,
    SidesTopBottom,
    SixFaces, 
    None
}



