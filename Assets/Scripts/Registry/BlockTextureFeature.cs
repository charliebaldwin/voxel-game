using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class BlockTextureFeature
{

    public int FeatureAtlas;
    public List<int> TextureIndices;
    public abstract List<Texture2D> GetTextures();
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

}

public class GrassTextureFeature : BlockTextureFeature
{
    public Texture2D GrassSideTexture;
    public Texture2D GrassTopTexture;

    public override List<Texture2D> GetTextures()
    {
        return new List<Texture2D> { GrassSideTexture, GrassTopTexture };
    }
    public override int GetIndexNX() { return TextureIndices[0]; }
    public override int GetIndexPX() { return TextureIndices[0]; }
    public override int GetIndexNY() { return -1; }
    public override int GetIndexPY() { return TextureIndices[1]; }
    public override int GetIndexNZ() { return TextureIndices[0]; }
    public override int GetIndexPZ() { return TextureIndices[0]; }
    public GrassTextureFeature()
    {
        GrassSideTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Texture_Features/Grass/grass-silt_s.png");
        GrassTopTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Texture_Features/Grass/grass-silt_t.png");
        TextureIndices = new List<int>();
    }
}