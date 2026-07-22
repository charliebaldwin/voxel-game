using UnityEngine;
using VInspector.Libs;

public class OrthoNormal
{
    public sbyte x;
    public sbyte y;
    public sbyte z;

    #region CONSTRUCTORS
    public OrthoNormal(int x, int y, int z)
    {
        this.x = (sbyte)Mathf.Clamp(x, -1, 1);
        this.y = (sbyte)Mathf.Clamp(y, -1, 1);
        this.z = (sbyte)Mathf.Clamp(z, -1, 1);
    }
    public OrthoNormal(sbyte x, sbyte y, sbyte z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public OrthoNormal(Vector3 vec)
    {
        x = (sbyte)Mathf.Clamp(vec.x, -1f, 1f).RoundToInt();
        y = (sbyte)Mathf.Clamp(vec.y, -1f, 1f).RoundToInt();
        z = (sbyte)Mathf.Clamp(vec.z, -1f, 1f).RoundToInt();
    }
    public OrthoNormal(Vector3Int vec)
    {
        x = (sbyte)Mathf.Clamp(vec.x, -1, 1);
        y = (sbyte)Mathf.Clamp(vec.y, -1, 1);
        z = (sbyte)Mathf.Clamp(vec.z, -1, 1);
    }

    #endregion

    #region CONVERSION
    public Vector3Int ToVector()
    {
        return new Vector3Int(x, y, z);
    }
    public static OrthoNormal FromVector(Vector3 vec)
    {
        return new OrthoNormal((sbyte)vec.x.RoundToInt(), (sbyte)vec.y.RoundToInt(), (sbyte)vec.z.RoundToInt());
    }
    public static OrthoNormal FromVector(Vector3Int vec)
    {
        return new OrthoNormal((sbyte)vec.x, (sbyte)vec.y, (sbyte)vec.z);
    }
    public override string ToString()
    {
        return $"ortho:[{x}, {y}, {z}]";
    }
    #endregion

    #region TRANSFORM
    public OrthoNormal Rotate(OrthoNormal axis, int quarterTurns)
    {
        Quaternion q = Quaternion.AngleAxis(90f * quarterTurns, axis.ToVector());
        Vector3 vec = this.ToVector();
        vec = q * vec;
        return FromVector(vec);
    }
    public OrthoNormal AlignYZ(OrthoNormal upAxis, OrthoNormal fwdAxis)
    {
        Quaternion q1 = Quaternion.FromToRotation(OrthoNormal.up.ToVector(), upAxis.ToVector());
        Quaternion q2 = Quaternion.FromToRotation(OrthoNormal.forward.ToVector(), fwdAxis.ToVector());
        return FromVector(q1 * q2 * this.ToVector());
    }

    public OrthoNormal Flip()
    {
        return new OrthoNormal(-x, -y, -z);
    }

    public Direction AsDirection()
    {
        if (IsEqual(left))
        {
            return Direction.NegativeX;
        }
        else if (IsEqual(right))
        {
            return Direction.PositiveX;
        } 
        else if (IsEqual(down))
        {
            return Direction.NegativeY;
        }
        else if (IsEqual(up))
        {
            return Direction.PositiveY;
        }
        else if (IsEqual(back))
        {
            return Direction.NegativeZ;
        }
        else if (IsEqual(forward))
        {
            return Direction.PositiveZ;
        }
        return Direction.Invalid;
    }
    #endregion

    public bool IsEqual(OrthoNormal other)
    {
        return x == other.x && y == other.y && z == other.z;
    }

    public static OrthoNormal left      = new OrthoNormal(-1, 0, 0);
    public static OrthoNormal right     = new OrthoNormal( 1, 0, 0);
    public static OrthoNormal down      = new OrthoNormal( 0,-1, 0);
    public static OrthoNormal up        = new OrthoNormal( 0, 1, 0);
    public static OrthoNormal back      = new OrthoNormal( 0, 0,-1);
    public static OrthoNormal forward   = new OrthoNormal( 0, 0, 1);
}