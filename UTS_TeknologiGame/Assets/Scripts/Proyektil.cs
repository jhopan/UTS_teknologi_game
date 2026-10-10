using UnityEngine;
using UnityEngine.Rendering;

/// Peluru yang terlihat terbang dari ujung laras ke titik kena.
/// Hanya tampilan: hasil tembakan tetap ditentukan raycast. Tanpa collider.
public class Proyektil : MonoBehaviour
{
    Vector3 tujuan;
    float kecepatan = 90f;
    float umurMaks = 3f;

    bool pakaiEfekKena = true;
    float durasiEfekKena = 0.18f;

    bool sudahSampai;
    float umur;

    /// Menyiapkan tujuan, kecepatan, dan tampilan proyektil.
    public void Mulai(Vector3 titikTujuan, float kecepatanTerbang,
        bool jejakAktif, float durasiJejak, float lebarJejak, Color warnaJejak,
        bool efekKena, float durasiEfek)
    {
        tujuan = titikTujuan;
        kecepatan = Mathf.Max(1f, kecepatanTerbang);
        pakaiEfekKena = efekKena;
        durasiEfekKena = Mathf.Max(0.02f, durasiEfek);

        // proyektil tidak perlu terbang lebih lama dari waktu tempuh wajar
        float jarak = Vector3.Distance(transform.position, titikTujuan);
        umurMaks = Mathf.Max(0.5f, jarak / kecepatan + 0.5f);

        Vector3 arah = titikTujuan - transform.position;
        if (arah.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(arah.normalized, Vector3.up);

        if (jejakAktif) SiapkanJejak(durasiJejak, lebarJejak, warnaJejak);
    }

    void SiapkanJejak(float durasiJejak, float lebarJejak, Color warna)
    {
        TrailRenderer jejak = gameObject.AddComponent<TrailRenderer>();
        jejak.time = Mathf.Max(0.02f, durasiJejak);
        jejak.startWidth = Mathf.Max(0.01f, lebarJejak);
        jejak.endWidth = 0f;
        jejak.minVertexDistance = 0.05f;
        jejak.autodestruct = false;
        jejak.shadowCastingMode = ShadowCastingMode.Off;
        jejak.receiveShadows = false;
        jejak.numCapVertices = 2;

        Material m = AsetVisual.MaterialJejak();
        if (m != null) jejak.sharedMaterial = m;

        jejak.startColor = new Color(warna.r, warna.g, warna.b, 0.95f);
        jejak.endColor = new Color(warna.r, warna.g, warna.b, 0f);
        jejak.emitting = true;
    }

    void Update()
    {
        if (sudahSampai) return;

        umur += Time.deltaTime;
        if (umur > umurMaks)
        {
            // jaring pengaman supaya proyektil tidak menumpuk kalau tujuan tak tercapai
            Hancur(false);
            return;
        }

        Vector3 posisiBaru = Vector3.MoveTowards(transform.position, tujuan, kecepatan * Time.deltaTime);
        bool sampai = (posisiBaru - tujuan).sqrMagnitude < 0.0004f;   // ~2 cm dari tujuan
        transform.position = posisiBaru;

        if (sampai) Hancur(true);
    }

    void Hancur(bool buatEfekKena)
    {
        if (sudahSampai) return;
        sudahSampai = true;

        if (buatEfekKena && pakaiEfekKena)
            EfekKena.Buat(tujuan, durasiEfekKena);

        // jejak dipisah dari proyektil supaya tidak terpotong saat proyektil hilang
        if (GetComponent<TrailRenderer>() != null)
        {
            transform.DetachChildren();
            TrailRenderer jejak = GetComponent<TrailRenderer>();
            jejak.transform.SetParent(null, true);
            Destroy(jejak.gameObject, jejak.time + 0.1f);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
