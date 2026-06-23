using UnityEngine;
using UnityEngine.Profiling;
using VInspector;

public class TestScript : MonoBehaviour
{
    private void Update()
    {
        RunTest();
    }
    [Button]
    unsafe void RunTest()
    {
        Profiler.BeginSample("CharlieSample");
        Debug.Log($"byte={sizeof(byte)}bytes");
        Debug.Log($"int={sizeof(int)}bytes");
        Debug.Log($"long={sizeof(long)}bytes");
        Debug.Log($"float={sizeof(float)}bytes");
        Debug.Log($"Vector3Int={sizeof(Vector3Int)}bytes");
        Debug.Log($"OrthoNormal={sizeof(OrthoNormal)}bytes");

        OrthoNormal o = new OrthoNormal(0, 1, 0);
        Debug.Log($"original: ({o.x}, {o.y}, {o.z})");
        OrthoNormal r = RotateOrtho(o, Quaternion.Euler(90f, 0f, 0f));
        Debug.Log($"rotated: ({r.x}, {r.y}, {r.z})");
        Profiler.EndSample();
    }

    public static OrthoNormal RotateOrtho(OrthoNormal original, Quaternion rotation)
    {
        Vector3 vec = new Vector3Int(original.x, original.y, original.z);    
        vec = rotation * vec;

        return new OrthoNormal(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y), Mathf.RoundToInt(vec.z));
    }
}

public struct OrthoNormal
{
    public sbyte x;
    public sbyte y;
    public sbyte z;

    public OrthoNormal(int x, int y, int z)
    {
        this.x = (sbyte)Mathf.Clamp(x, -1, 1);
        this.y = (sbyte)Mathf.Clamp(y, -1, 1); 
        this.z = (sbyte)Mathf.Clamp(z, -1, 1);
    }
}