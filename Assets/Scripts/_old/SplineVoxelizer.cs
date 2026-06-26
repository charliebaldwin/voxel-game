using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using VInspector;
using VInspector.Libs;

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

            Vector3 tangent = new Vector3(splineTan.x.Round(), splineTan.y.Round(), splineTan.z.Round());
            Vector3 up = new Vector3(splineUp.x.Round(), splineUp.y.Round(), splineUp.z.Round());   
            Vector3 normal = Vector3.Cross(tangent, up).normalized;
            normal = new Vector3(normal.x.Round(), normal.y.Round(), normal.z.Round());


            Vector3Int voxel = new Vector3Int(splinePos.x.RoundToInt(), splinePos.y.RoundToInt(), splinePos.z.RoundToInt());
            if (!SplineVoxels.Contains(voxel)) {
                SplineVoxels.Add(voxel);
                SplineVoxels.Add(voxel + new Vector3Int(up.x.RoundToInt(), up.y.RoundToInt(), up.z.RoundToInt()));
                SplineVoxels.Add(voxel - new Vector3Int(up.x.RoundToInt(), up.y.RoundToInt(), up.z.RoundToInt()));
                SplineVoxels.Add(voxel + new Vector3Int(normal.x.RoundToInt(), normal.y.RoundToInt(), normal.z.RoundToInt()));
                SplineVoxels.Add(voxel - new Vector3Int(normal.x.RoundToInt(), normal.y.RoundToInt(), normal.z.RoundToInt()));
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
