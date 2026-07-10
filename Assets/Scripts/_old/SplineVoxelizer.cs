using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using VInspector;
//using VInspector.Libs;

public class SplineVoxelizer : MonoBehaviour
{
    public SplineContainer SplineCutter;
    public float StepSize = 0.5f;
    public int Thickness = 1;
    public List<Vector3Int> SplineVoxels;

    [Button]
    public List<Vector3Int> VoxelizeSpline()
    {
        SplineVoxels = new List<Vector3Int>();
        Spline spline = SplineCutter.Spline;
        float length = SplineCutter.CalculateLength();

        for (float t = 0f; t < length; t += StepSize)
        {
            float3 splinePos = SplineCutter.EvaluatePosition(t);
            float3 splineTan = SplineCutter.EvaluateTangent(t);
            float3 splineUp = SplineCutter.EvaluateUpVector(t);

            Vector3 tangent = new Vector3(Mathf.Round(splineTan.x), Mathf.Round(splineTan.y), Mathf.Round(splineTan.z));
            Vector3 up = new Vector3(Mathf.Round(splineUp.x), Mathf.Round(splineUp.y), Mathf.Round(splineUp.z));   
            Vector3 normal = Vector3.Cross(tangent, up).normalized;
            normal = new Vector3(Mathf.Round(normal.x), Mathf.Round(normal.y), Mathf.Round(normal.z));


            Vector3Int voxel = new Vector3Int(Mathf.RoundToInt(splinePos.x), Mathf.RoundToInt(splinePos.y), Mathf.RoundToInt(splinePos.z));
            if (!SplineVoxels.Contains(voxel)) {
                SplineVoxels.Add(voxel);
                SplineVoxels.Add(voxel + new Vector3Int(Mathf.RoundToInt(up.x), Mathf.RoundToInt(up.y), Mathf.RoundToInt(up.z)));
                SplineVoxels.Add(voxel - new Vector3Int(Mathf.RoundToInt(up.x), Mathf.RoundToInt(up.y), Mathf.RoundToInt(up.z)));
                SplineVoxels.Add(voxel + new Vector3Int(Mathf.RoundToInt(normal.x), Mathf.RoundToInt(normal.y), Mathf.RoundToInt(normal.z)));
                SplineVoxels.Add(voxel - new Vector3Int(Mathf.RoundToInt(normal.x), Mathf.RoundToInt(normal.y), Mathf.RoundToInt(normal.z)));
            }
        }
        return SplineVoxels;
    }
    private void OnDrawGizmos()
    {
        if (SplineVoxels != null) {
            foreach (Vector3 v in SplineVoxels) { 
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(v, Vector3.one);
            }
        }
    }


}
