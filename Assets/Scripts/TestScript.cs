using UnityEngine;
using UnityEngine.Profiling;
using VInspector;

public class TestScript : MonoBehaviour
{
    private void Start()
    {
        RunTest();

    }
    private void Update()
    {

    }

    [Button]
    unsafe void RunTest()
    {
        OrthoNormal o = new OrthoNormal(1, 0, 0);
        Debug.Log($"original: {o}");

        o = o.Rotate(OrthoNormal.up, 1);
        Debug.Log($"rotated on y 1x: {o}");
        o = o.Rotate(OrthoNormal.up, 1);
        Debug.Log($"rotated on y 2x: {o}");
        o = o.Rotate(OrthoNormal.up, 1);
        Debug.Log($"rotated on y 3x: {o}");

        o = o.Rotate(OrthoNormal.left, 1);
        Debug.Log($"rotated on x 1x: {o}");
        o = o.Rotate(OrthoNormal.left, 1);
        Debug.Log($"rotated on x 2x: {o}");
        o = o.Rotate(OrthoNormal.left, 1);
        Debug.Log($"rotated on x 3x: {o}");
    }

    public static OrthoNormal RotateOrtho(OrthoNormal original, Quaternion rotation)
    {
        Vector3 vec = new Vector3Int(original.x, original.y, original.z);    
        vec = rotation * vec;

        return new OrthoNormal(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y), Mathf.RoundToInt(vec.z));
    }
}

