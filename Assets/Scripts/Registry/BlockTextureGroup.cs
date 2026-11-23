using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class BlockTextureGroup
{
    public List<Texture2D> Textures;
    public List<int> TextureIndices;
    public abstract int GetIndexNX();
    public abstract int GetIndexPX();
    public abstract int GetIndexNY();
    public abstract int GetIndexPY();
    public abstract int GetIndexNZ();
    public abstract int GetIndexPZ();
    public int[] GetIndices()
    {
        return new int[6] { GetIndexNX(), GetIndexPX(), GetIndexNY(), GetIndexPY(), GetIndexNZ(), GetIndexPZ() };
    }
    public virtual int[] GetRandomIndices(int seed)
    {
        int len = Textures.Count;
        return (new int[6] {
            TextureIndices[(seed) % len],
            TextureIndices[(seed+1) % len],
            TextureIndices[(seed+2) % len],
            TextureIndices[(seed+3) % len],
            TextureIndices[(seed+4) % len],
            TextureIndices[(seed+5) % len]
        }).ToArray<int>();

    }



}

public class UniformBlockTextureGroup : BlockTextureGroup
{

    public override int GetIndexNX() { return TextureIndices[0]; }
    public override int GetIndexPX() { return TextureIndices[0]; }
    public override int GetIndexNY() { return TextureIndices[0]; }
    public override int GetIndexPY() { return TextureIndices[0]; }
    public override int GetIndexNZ() { return TextureIndices[0]; }
    public override int GetIndexPZ() { return TextureIndices[0]; }
    public UniformBlockTextureGroup()
    {
        Textures = new List<Texture2D>();
        TextureIndices = new List<int>();
        Textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Block_Default_Color.png"));
    }
}

public class SixSidedBlockTextureGroup : BlockTextureGroup
{

    public override int GetIndexNX() { return TextureIndices[0]; }
    public override int GetIndexPX() { return TextureIndices[1]; }
    public override int GetIndexNY() { return TextureIndices[2]; }
    public override int GetIndexPY() { return TextureIndices[3]; }
    public override int GetIndexNZ() { return TextureIndices[4]; }
    public override int GetIndexPZ() { return TextureIndices[5]; }

    public SixSidedBlockTextureGroup()
    {
        Textures = new List<Texture2D>();
        TextureIndices = new List<int>();
        for (int i = 0; i < 6; i++)
            Textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Block_Default_Color.png"));
    }
}
public class LogBlockTextureGroup : BlockTextureGroup
{
    public override int GetIndexNX() { return TextureIndices[0]; }
    public override int GetIndexPX() { return TextureIndices[0]; }
    public override int GetIndexNY() { return TextureIndices[1]; }
    public override int GetIndexPY() { return TextureIndices[1]; }
    public override int GetIndexNZ() { return TextureIndices[0]; }
    public override int GetIndexPZ() { return TextureIndices[0]; }
    public LogBlockTextureGroup()
    {
        Textures = new List<Texture2D>();
        TextureIndices = new List<int>();
        for (int i = 0; i < 2; i++)
            Textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Block_Default_Color.png"));
    }
    public override int[] GetRandomIndices(int seed)
    {
        return GetIndices();
    }
}