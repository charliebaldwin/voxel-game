using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VInspector;

public class AppendBufferTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public Vector3Int Size = new Vector3Int(3, 3, 3);
    public Vector3Int GroupSize = new Vector3Int(3, 3, 3);
    private int size3d;

    private ComputeBuffer appendBuffer;
    private ComputeBuffer voxelBuffer;
    private ComputeBuffer staticBuffer;
    private List<Vector3> vertexData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        GenerateVertices();
        ReadData();
    }

    private void OnDrawGizmos()
    {
        foreach(Vector3 v  in vertexData)
        {
            Gizmos.DrawCube(v, Vector3.one * 0.2f);
        }
    }

    [Button(name = "Generate", size = 20, color = "black")]
    void GenerateVertices()
    {
        size3d = Size.x * Size.y * Size.z;

        appendBuffer = new ComputeBuffer(size3d, 3 * sizeof(float), ComputeBufferType.Append);
        appendBuffer.SetCounterValue(0);

        staticBuffer = new ComputeBuffer(size3d, 3 * sizeof(float), ComputeBufferType.Counter);
        staticBuffer.SetCounterValue(0);

        computeShader.SetBuffer(0, "StaticBuffer", staticBuffer);
        computeShader.SetVector("Size", new Vector4(Size.x, Size.y, Size.z, 0.0f));
        computeShader.Dispatch(0, GroupSize.x, GroupSize.y, GroupSize.z);

        
    }

    [Button(name = "ReadData", size = 20, color = "black")]
    void ReadData()
    {

        Vector3[] vertexDataArray = new Vector3[size3d];
        staticBuffer.GetData(vertexDataArray);
        vertexData = vertexDataArray.ToList();

        Vector3 offset = vertexDataArray[0];

        //for (int i = 0; i < vertexData.Count; i++)
        //{
        //    //vertexData[i] = vertexData[i] - offset;
        //    Debug.Log(vertexData[i]);
             
        //}
    }

}
