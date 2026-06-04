using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenSettings", menuName = "Scriptable Objects/WorldGenSettings")]
public class WorldGenSettings : ScriptableObject
{
    public float NoiseScale = 1f;
    public int NoiseOctaves = 2;
    public float HeightRange = 4f;
    public float HeightOffset = 10f;
}
