using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// Mesh dan material siap pakai untuk objek tampilan (proyektil, efek kena).
/// Objek dibuat tanpa collider supaya tidak tertangkap raycast tembakan.
public static class AsetVisual
{
    static Mesh meshBulat;
    static readonly Dictionary<Color, Material> cacheMenyala = new Dictionary<Color, Material>();
    static Material materialJejak;

    /// Mesh bola bersama. Diambil dari primitive sementara lalu collider-nya dibuang.
    public static Mesh MeshBulat()
    {
        if (meshBulat == null)
        {
            GameObject sementara = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider c = sementara.GetComponent<Collider>();
            if (c != null) c.enabled = false;

            meshBulat = sementara.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sementara);
        }
        return meshBulat;
    }

    /// Objek bola menyala tanpa collider.
    public static GameObject BuatObjekMenyala(string nama, Color warna)
    {
        GameObject go = new GameObject(nama);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = MeshBulat();

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MaterialMenyala(warna);
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return go;
    }

    /// Material menyala (emission), di-cache per warna.
    public static Material MaterialMenyala(Color warna)
    {
        Material ada;
        if (cacheMenyala.TryGetValue(warna, out ada) && ada != null) return ada;

        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Unlit/Color");

        Material m = sh != null ? new Material(sh) : null;
        if (m != null)
        {
            m.name = "MaterialMenyala";
            m.color = warna;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", warna * 1.8f);
            }
        }

        cacheMenyala[warna] = m;
        return m;
    }

    /// Material transparan untuk jejak proyektil.
    public static Material MaterialJejak()
    {
        if (materialJejak != null) return materialJejak;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Standard");

        if (sh != null)
        {
            materialJejak = new Material(sh);
            materialJejak.name = "MaterialJejakProyektil";
        }
        return materialJejak;
    }
}
