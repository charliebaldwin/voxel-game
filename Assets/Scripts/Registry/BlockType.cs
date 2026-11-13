using System.Collections.Generic;
using Sirenix.OdinInspector.Editor.Internal;
using UnityEngine;

public class BlockType
{
    public string ID;
    public BlockTextureGroup TextureGroup;
    public List<BlockTextureFeature> TextureFeatures;
    public int Toughness;
    public List<Color> Colors;

    public BlockType()
    {
        ID = "null";
        TextureGroup = new UniformBlockTextureGroup();
        TextureFeatures = new List<BlockTextureFeature>();
        Toughness = 1;
        Colors = new List<Color> { Color.white };
    }

}