using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class BlockTextureFeature
{

    public int FeatureAtlas;
    public List<int> TextureIndices;
    public abstract List<Texture2D> GetTextures();

}

public class GrassTextureFeature : BlockTextureFeature
{
    public Texture2D GrassSideTexture;
    public Texture2D GrassTopTexture;

    public override List<Texture2D> GetTextures()
    {
        return new List<Texture2D> { GrassSideTexture, GrassTopTexture };
    }
    public GrassTextureFeature()
    {
        GrassSideTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Texture_Features/Grass/grass-silt_s.png");
        GrassTopTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Blocks/Texture_Features/Grass/grass-silt_t.png");
        TextureIndices = new List<int>();
    }
}