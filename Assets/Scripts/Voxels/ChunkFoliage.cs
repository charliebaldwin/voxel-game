using NUnit.Framework;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ChunkFoliage : MonoBehaviour
{
    public List<Vector3> GrassPositions = new List<Vector3>();
    private GraphicsBuffer posBuffer;// = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 0, 3 * sizeof(float));

    public VisualEffect vfx;

    private void OnValidate()
    {
        //LoadFoliage();
    }

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1000, 3 * sizeof(float));
    }
    public void Load()
    {
       // LoadFoliage();
    }

    public void Unload()
    {
       // vfx.Stop();
        //posBuffer.Release();
    }


    public void SetFoliage(List<Vector3> positions)
    {
        GrassPositions = positions;
        posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1000, 3 * sizeof(float));
        posBuffer.SetData(GrassPositions);
        LoadFoliage();
    }

    [Button]
    public void LoadFoliage()
    {
        if (GrassPositions.Count > 0)
        {
            posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GrassPositions.Count, 3 * sizeof(float));
            posBuffer.SetData(GrassPositions);
            vfx.SetGraphicsBuffer("Positions", posBuffer);
            vfx.Reinit();
            vfx.Play();
        } else
        {
            vfx.Stop();
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
