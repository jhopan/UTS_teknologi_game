using UnityEngine;

/// Ledakan kecil di titik kena: bola menyala yang membesar lalu mengecil dan hilang.
public class EfekKena : MonoBehaviour
{
    float durasi = 0.18f;
    float umur;
    float ukuranAwal = 0.06f;
    float ukuranPuncak = 0.42f;
    Vector3 skalaAsli;
    Material material;
    Color warnaAsli;

    /// Membuat satu efek kena di posisi tertentu.
    public static EfekKena Buat(Vector3 posisi, float durasi)
    {
        GameObject go = AsetVisual.BuatObjekMenyala("EfekKena", new Color(1f, 0.78f, 0.25f));
        go.transform.position = posisi;

        EfekKena efek = go.AddComponent<EfekKena>();
        efek.durasi = Mathf.Max(0.05f, durasi);
        efek.skalaAsli = Vector3.one * efek.ukuranAwal;
        go.transform.localScale = efek.skalaAsli;

        efek.material = go.GetComponent<Renderer>() != null ? go.GetComponent<Renderer>().material : null;
        if (efek.material != null) efek.warnaAsli = efek.material.color;

        return efek;
    }

    void Update()
    {
        umur += Time.deltaTime;

        float t = Mathf.Clamp01(umur / durasi);

        // membesar cepat di awal, lalu mengecil sampai hilang
        float skala = t < 0.25f
            ? Mathf.Lerp(ukuranAwal, ukuranPuncak, t / 0.25f)
            : Mathf.Lerp(ukuranPuncak, 0f, (t - 0.25f) / 0.75f);

        transform.localScale = skalaAsli.normalized * skala;

        if (material != null)
        {
            Color c = warnaAsli;
            c.a = 1f - t;
            material.color = c;
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", warnaAsli * (1.8f * (1f - t)));
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
