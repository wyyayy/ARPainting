using System;
using System.Diagnostics;
using UnityEngine;
using System.Runtime.InteropServices;

public class MathEx
{
	public const int MAX_INT =  2147483647;	 // 2 ^ (31 - 1)
    public const int MIN_INT = -2147483648;	 // -2 ^ 31

    public const int INFINITE = MAX_INT;
    public const float FLOAT_INFINITE = float.PositiveInfinity;

    [StructLayout(LayoutKind.Explicit)]
    private struct FloatIntUnion
    {
        [FieldOffset(0)]
        public float f;

        [FieldOffset(0)]
        public int tmp;
    }

/*
    Abandoned since it is slower than Mathf.Sqrt   
    public static float Sqrt(float z)
    {
        if (z == 0) return 0;
        FloatIntUnion u;
        u.tmp = 0;
        float xhalf = 0.5f * z;
        u.f = z;
        u.tmp = 0x5f375a86 - (u.tmp >> 1);
        u.f = u.f * (1.5f - xhalf * u.f * u.f);
        return u.f * z;
    }
*/

    public static float Distance(Vector3 v1, Vector3 v2)
    {
        float fX = v1.x - v2.x;
        float fY = v1.y - v2.y;
        float fZ = v1.z - v2.z;

        return Mathf.Sqrt(fX * fX + fY * fY + fZ * fZ);
    }

    public static float DistanceSquareXZ (float x1, float y1, float x2, float y2)
	{
		return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
	}

    public static float DistanceXZ(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public static float DistanceSquareXZ(Vector3 v1, Vector3 v2)
    {
        float fX = v1.x - v2.x;
        float fZ = v1.z - v2.z;
        return fX * fX + fZ * fZ;
    }

    public static float DistanceXZ(Vector3 v1, Vector3 v2)
    {
        float fX = v1.x - v2.x;
        float fZ = v1.z - v2.z;
        return Mathf.Sqrt(fX * fX + fZ * fZ);
    }

    public static float DistanceSquare(Vector3 v1, Vector3 v2)
    {
        float fX = v1.x - v2.x;
        float fY = v1.y - v2.y;
        float fZ = v1.z - v2.z;
        return fX * fX + fY * fY + fZ * fZ;
    }
    
    /// None GC maths
    public static Vector3 V3Add(Vector3 v1, Vector3 v2, ref Vector3 vOut)
    {
        vOut.Set(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        return vOut;
    }

    /// Will remove the Y axis
    public static Vector3 V3MinusXZ(Vector3 v1, Vector3 v2, ref Vector3 vOut)
    {
        vOut.Set(v1.x - v2.x, 0, v1.z - v2.z);
        return vOut;
    }

    public static Vector3 V3Minus(Vector3 v1, Vector3 v2, ref Vector3 vOut)
    {
        vOut.Set(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        return vOut;
    }

    public static Vector3 V3DivideXZ(Vector3 v, float fValue, ref Vector3 vOut)
    {
        vOut.Set(v.x / fValue, 0, v.z / fValue);
        return vOut;
    }

    public static Vector3 V3Divide(Vector3 v, float fValue, ref Vector3 vOut)
    {
        vOut.Set(v.x / fValue, v.y / fValue, v.z / fValue);
        return vOut;
    }

    public static Vector3 V3Multiply(Vector3 v1, float fValue, ref Vector3 vOut)
    {
        vOut.Set(v1.x * fValue, v1.y * fValue, v1.z * fValue);
        return vOut;
    }

    ///-------------

}
	
