using UnityEngine;
using UnityEngine.VFX;
using VInspector;

public class VFXObject : MonoBehaviour
{
    public bool ShouldDestroy = false;
    private VisualEffect VFX;
    private float duration = 3f;

    [ShowInInspector]
    private float t;

    public void InitVFX(int blockType, float duration)
    {
        this.duration = duration;
        VFX = GetComponent<VisualEffect>();
        VFX.SetInt("BlockType", blockType);
        VFX.SetFloat("Duration", this.duration);
        t = 0f;
    }
    public void InitVFX(int blockType, float duration, Vector3 normal)
    {
        this.duration = duration;
        VFX = GetComponent<VisualEffect>();
        VFX.SetInt("BlockType", blockType);
        VFX.SetFloat("Duration", this.duration);
        VFX.SetVector3("Normal", normal);
        t = 0f;
    }
    // Update is called once per frame
    void Update()
    {
        t += Time.deltaTime;

        if (t > duration)// && VFX.aliveParticleCount <= 0)
        {
            if (VFX != null)
            {
                ShouldDestroy = true;
                Destroy(gameObject);
            }
        }
    }
}
