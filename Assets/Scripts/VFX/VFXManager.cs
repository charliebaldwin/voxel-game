//using UnityEditor.PackageManager;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    public int MaxVFXObjects;

    public GameObject VFXBlockBreak;
    public GameObject VFXBlockDamage;


    private void Awake()
    {
        SetInstance();
    }


    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public void SpawnVFX(VFXType type, Vector3 position, Vector3 normal, int id)
    {
        int vfxCount = transform.childCount;
        if (vfxCount < MaxVFXObjects)
        {
            GameObject newVFX = null;
            float duration = 1f;
            switch (type)
            {
                case VFXType.BLOCK_BREAK:
                    newVFX = Instantiate(VFXBlockBreak, position, Quaternion.identity);
                    duration = 0.5f;
                    break;
                case VFXType.BLOCK_DMG:
                    newVFX = Instantiate(VFXBlockDamage, position, Quaternion.identity);
                    duration = 1f;
                    break;
            }
            if (newVFX != null) {
                newVFX.transform.SetParent(transform, true);
                newVFX.GetComponent<VFXObject>().InitVFX(id, duration, normal);

            }
        }
    
    }
}

public enum VFXType
{
    BLOCK_BREAK,
    BLOCK_DMG,
}
