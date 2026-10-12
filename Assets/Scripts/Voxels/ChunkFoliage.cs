using NUnit.Framework;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ChunkFoliage : MonoBehaviour
{
    public List<Vector3> TestPosList = new List<Vector3>();

    public VisualEffect vfx;

    private void OnValidate()
    {
        //LoadFoliage();
    }

    [Button]
    public void LoadFoliage()
    {
        Debug.Log("starting LoadFoliage()");
        
        vfx = GetComponent<VisualEffect>();
        Debug.Log("loaded vfx");
        
        GraphicsBuffer posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, TestPosList.Count, 3*sizeof(float));
        Debug.Log("created buffer");
        
        posBuffer.SetData(TestPosList);
        Debug.Log("set data in buffer");
        
        vfx.SetGraphicsBuffer("Positions", posBuffer);
        Debug.Log("set vfx buffer");

        vfx.Reinit();
        Debug.Log("reinit'd vfx");

        vfx.Play();
        Debug.Log("played vfx");
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
