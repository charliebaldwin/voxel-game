using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Sirenix;
using Sirenix.OdinInspector;
using System.IO;

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
        //OrthoNormal o = new OrthoNormal(1, 0, 0);
        //Debug.Log($"original: {o}");

        //o = o.Rotate(OrthoNormal.up, 1);
        //Debug.Log($"rotated on y 1x: {o}");
        //o = o.Rotate(OrthoNormal.up, 1);
        //Debug.Log($"rotated on y 2x: {o}");
        //o = o.Rotate(OrthoNormal.up, 1);
        //Debug.Log($"rotated on y 3x: {o}");

        //o = o.Rotate(OrthoNormal.left, 1);
        //Debug.Log($"rotated on x 1x: {o}");
        //o = o.Rotate(OrthoNormal.left, 1);
        //Debug.Log($"rotated on x 2x: {o}");
        //o = o.Rotate(OrthoNormal.left, 1);
        //Debug.Log($"rotated on x 3x: {o}");
    }

    public static OrthoNormal RotateOrtho(OrthoNormal original, Quaternion rotation)
    {
        Vector3 vec = new Vector3Int(original.x, original.y, original.z);    
        vec = rotation * vec;

        return new OrthoNormal(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y), Mathf.RoundToInt(vec.z));
    }

    public OrthoNormal[] allOrthos =
    {
        OrthoNormal.left, OrthoNormal.right, OrthoNormal.down, OrthoNormal.up, OrthoNormal.back, OrthoNormal.forward
    };

    public Dictionary<OrthoNormal, string> orthoNames = new Dictionary<OrthoNormal, string>
        {
            { OrthoNormal.left, "left" },
            { OrthoNormal.right, "right" },
            { OrthoNormal.down, "down" },
            { OrthoNormal.up, "up" },
            { OrthoNormal.back, "back" },
            { OrthoNormal.forward, "fwd" }
        };
    [Button]
    public void OrthoNormalTest()
    {
        StreamWriter writer = new StreamWriter("./orthotest.txt");
        using (writer)
        {
            int oi = 0, ui = 0, fi = 0;
            foreach (OrthoNormal o in orthoNames.Keys)
            {
                ui = 0;
                foreach (OrthoNormal up in orthoNames.Keys)
                {
                    fi = 0;
                    foreach (OrthoNormal fwd in orthoNames.Keys)
                    {
                        OrthoNormal result = o.AlignYZ(up, fwd);
                        int i = 0;
                        if (result.IsEqual(OrthoNormal.left)) i = 0;
                        else if (result.IsEqual(OrthoNormal.right)) i = 1;
                        else if (result.IsEqual(OrthoNormal.down)) i = 2;
                        else if (result.IsEqual(OrthoNormal.up)) i = 3;
                        else if (result.IsEqual(OrthoNormal.back)) i = 4;
                        else if (result.IsEqual(OrthoNormal.forward)) i = 5;

                        string on = orthoNames[allOrthos[oi]];
                        string un = orthoNames[allOrthos[ui]];
                        string fn = orthoNames[allOrthos[fi]];
                        //                    Debug.Log($"original={on}, up={un}, fwd={fn} ===> {orthoNames[allOrthos[i]]}");
                        Debug.Log($"else if(o.IsEqual(OrthoNormal.{on}) && u.IsEqual(OrthoNormal.{un}) && f.isEqual(OrthoNormal.{fn})) return {orthoNames[allOrthos[i]]};");
                        writer.WriteLine($"else if(o.IsEqual(OrthoNormal.{on}) && u.IsEqual(OrthoNormal.{un}) && f.isEqual(OrthoNormal.{fn})) return {orthoNames[allOrthos[i]]};");
                        fi++;
                    }
                    ui++;
                }
                oi++;
            }
            writer.Close();
        }
    }
}

