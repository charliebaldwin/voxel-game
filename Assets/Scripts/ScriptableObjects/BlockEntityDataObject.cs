using System;
using UnityEngine;

[Serializable]
public class BlockEntityData
{
    public BlockID BlockID;
    public Mesh EntityMesh;
    public Material EntityMaterial;
}

[CreateAssetMenu(fileName = "BlockEntityDataObject", menuName = "Scriptable Objects/BlockEntityDataObject")]
public class BlockEntityDataObject : ScriptableObject
{
    public BlockEntityData Data = new BlockEntityData();
}
