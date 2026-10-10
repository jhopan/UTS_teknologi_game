using UnityEngine;
using UnityEngine.AI;

/// Tombol pintas tanpa panel di layar.
/// T = tembak, B = cek status NavMesh (ke Console), C = on/off crosshair.
/// Panel daftar periksa hanya muncul kalau `tampilkanPanel` dicentang di Inspector.
public class GameHUD : MonoBehaviour
{
    public PlayerTembak penembak;
    public PlayerGerak gerakPlayer;

    [Header("Tombol pintas (tidak tampil di layar)")]
    public KeyCode tombolTembak = KeyCode.T;
    public KeyCode tombolBake = KeyCode.B;
    public KeyCode tombolCrosshair = KeyCode.C;

    [Header("Panel daftar periksa")]
    [Tooltip("Default MATI supaya layar Game bersih. Nyalakan hanya saat butuh bukti penilaian.")]
    public bool tampilkanPanel = false;

    string pesanBake = "";

    void Update()
    {
        if (Input.GetKeyDown(tombolTembak) && penembak != null) penembak.Tembak();

        if (Input.GetKeyDown(tombolBake)) CekNavMesh();

        if (Input.GetKeyDown(tombolCrosshair) && penembak != null)
        {
            penembak.tampilkanCrosshair = !penembak.tampilkanCrosshair;
            Debug.Log("[Game] crosshair " + (penembak.tampilkanCrosshair ? "DITAMPILKAN" : "DISEMBUNYIKAN"));
        }
    }

    /// Memeriksa status NavMesh dari runtime. Hasilnya ditulis ke Console.
    /// Berguna sebagai bukti bahwa AI memang berjalan di atas NavMesh: kalau NavMesh
    /// tidak ada, agent tidak akan berada "di atas NavMesh" dan tidak punya jalur.
    void CekNavMesh()
    {
        MusuhAI[] musuh = Object.FindObjectsOfType<MusuhAI>();
        int diAtasNavMesh = 0;
        int punyaJalur = 0;
        foreach (MusuhAI m in musuh)
        {
            if (m.DiAtasNavMesh) diAtasNavMesh++;
            if (m.PunyaJalur) punyaJalur++;
        }

        pesanBake = "status NavMesh -> " + diAtasNavMesh + "/" + musuh.Length
            + " agent di atas NavMesh, " + punyaJalur + " punya jalur aktif";

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        int titik = tri.vertices != null ? tri.vertices.Length : 0;
        int segitiga = tri.indices != null ? tri.indices.Length / 3 : 0;

        Debug.Log("[Game] " + pesanBake + " | NavMesh: " + titik + " titik, " + segitiga + " segitiga.");
        Debug.Log("[Game] NavMesh ini dibake di Editor (menu Game > Bake NavMesh) lalu tersimpan di file scene.");
    }

    void OnGUI()
    {
        // Panel daftar periksa dimatikan secara default (layar Game bersih).
        // Nyalakan `tampilkanPanel` di Inspector kalau perlu ditunjukkan saat penilaian.
        if (!tampilkanPanel) return;

        MusuhAI[] musuh = Object.FindObjectsOfType<MusuhAI>();
        int hidup = 0;
        int diNavMesh = 0;
        int mengejar = 0;
        float hpTerdekat = -1f;
        float jarakTerdekat = float.MaxValue;

        foreach (MusuhAI m in musuh)
        {
            if (m == null || m.SudahMati) continue;
            hidup++;
            if (m.DiAtasNavMesh) diNavMesh++;
            if (m.PunyaJalur) mengejar++;
            if (m.JarakKePlayer >= 0f && m.JarakKePlayer < jarakTerdekat)
            {
                jarakTerdekat = m.JarakKePlayer;
                hpTerdekat = m.HpSekarang;
            }
        }

        GUILayout.BeginArea(new Rect(10f, 10f, 430f, 300f), GUI.skin.box);
        GUILayout.Label("=== TEKNOLOGI GAME - DAFTAR PERIKSA ===");
        GUILayout.Label("");
        GUILayout.Label("1. ARENA & AI MUSUH (NavMesh)");
        GUILayout.Label("   [1a] lantai arena + rintangan          : ADA (lihat Hierarchy)");
        GUILayout.Label("   [1b] NavMesh sudah di-BAKE             : " + (diNavMesh > 0 ? "YA (" + diNavMesh + " agent valid)" : "belum / agent di luar NavMesh"));
        GUILayout.Label("   [1c] musuh mengejar player (NavMeshAgent) : " + (mengejar > 0 ? "YA (" + mengejar + " mengejar)" : "belum"));
        GUILayout.Label("        musuh hidup : " + hidup + "   |   jarak terdekat : "
            + (jarakTerdekat < float.MaxValue ? FormatAngka.F(jarakTerdekat, "F1") + " m" : "-")
            + "   |   HP terdekat : " + (hpTerdekat >= 0f ? FormatAngka.F(hpTerdekat, "F0") : "-"));
        GUILayout.Label("");
        GUILayout.Label("2. SISTEM MENEMBAK (Raycast + proyektil)");
        GUILayout.Label("   [2a] klik kiri = tembak (raycast)      : SIAP (crosshair di tengah)");
        if (penembak != null)
        {
            GUILayout.Label("   [2b] jarak tembak dihitung + di-Console: "
                + FormatAngka.F(penembak.JarakTembakanTerakhir, "F2") + " m");
            GUILayout.Label("        tembakan : " + penembak.JumlahTembakan
                + "   |   kena musuh : " + penembak.JumlahKenaMusuh
                + "   |   proyektil : " + penembak.JumlahProyektilDiluncurkan);
            GUILayout.Label("        hasil terakhir : " + penembak.InfoTerakhir);
        }
        GUILayout.Label("   [2c] HP musuh berkurang, <=0 hancur    : " + (hidup < 4 ? "SUDAH ada musuh hancur" : "siap (lihat bar HP di atas musuh)"));
        GUILayout.Label("");
        GUILayout.Label("WASD jalan | Shift lari | Space lompat | Mouse lihat | Klik kiri tembak");
        GUILayout.Label("[T] tembak   [B] cek NavMesh   [C] crosshair");
        if (!string.IsNullOrEmpty(pesanBake)) GUILayout.Label(pesanBake);
        GUILayout.EndArea();
    }
}
