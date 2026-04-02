using UnityEngine;
using UnityEngine.VFX;

public class VFXObject : MonoBehaviour
{

    private VisualEffect VFX;

    private float t;

    public void InitVFX(int blockType)
    {
        VFX = GetComponent<VisualEffect>();
        VFX.SetInt("BlockType", blockType);
        t = 0f;
    }
    // Update is called once per frame
    void Update()
    {
        t += Time.deltaTime;
        if (VFX != null)
        {
            if (t > 1f && VFX.aliveParticleCount <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
