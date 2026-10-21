using Unity.XR.CoreUtils;
using UnityEngine;

public class MagicCarpetPlane : MonoBehaviour
{
    private Vector3 origin;
    private Vector3 direction;
    private Vector3 offset;

    private void Awake()
    {
        direction = -1f * transform.up;
        origin = transform.position;// - direction;
        offset = -0.5f * direction;
    }
    public Vector3 GetOrigin()
    {
        return origin;
    }
    public Vector3 GetDirection()
    {
        return direction;
    }
    public Vector3 GetOffset()
    {
        return offset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawCube(origin, Vector3.one * 0.1f);
    }
}
