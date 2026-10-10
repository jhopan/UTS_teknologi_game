using System.Globalization;
using UnityEngine;

/// Penulisan angka dengan pemisah desimal titik, tidak ikut bahasa Windows.
public static class FormatAngka
{
    public static string F(float nilai, string format)
    {
        return nilai.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string V(Vector3 v, string format = "F2")
    {
        return "(" + F(v.x, format) + ", " + F(v.y, format) + ", " + F(v.z, format) + ")";
    }
}
