using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Pengambil screenshot beberapa sudut arena lewat Camera.Render().
/// Catatan: cara ini tidak menangkap OnGUI, jadi HUD tidak ikut terpotret.
/// Untuk HUD pakai ScreenshotHUD.
public static class ScreenshotBukti
{
    const string ScenePath = "Assets/Scenes/Arena.unity";
    const string FolderKeluar = "Bukti_Screenshot";

    static int langkah;
    static double langkahMulai;
    static double mulai;
    static int jumlahTersimpan;

    struct Sudut
    {
        public string nama;
        public Vector3 posisi;
        public Vector3 lihatKe;
        public string keterangan;

        public Sudut(string nama, Vector3 posisi, Vector3 lihatKe, string keterangan)
        {
            this.nama = nama; this.posisi = posisi; this.lihatKe = lihatKe; this.keterangan = keterangan;
        }
    }

    static readonly Sudut[] DaftarSudut =
    {
        new Sudut("01_arena_dari_atas",
            new Vector3(0f, 42f, -34f), new Vector3(0f, 0f, 0f),
            "Tampak keseluruhan arena 40x40 m, rintangan, Player (biru), 4 Musuh (merah)"),
        new Sudut("02_musuh_mengejar_player",
            new Vector3(-15f, 6f, -22f), new Vector3(-6f, 1f, -8f),
            "Musuh (NavMeshAgent) bergerak mendekati Player"),
        new Sudut("03_rintangan_berkelok",
            new Vector3(6f, 16f, -16f), new Vector3(4f, 0f, 2f),
            "Rintangan berkelok - jalur musuh memutar, bukan garis lurus"),
        new Sudut("04_sudut_pandang_player",
            new Vector3(-24f, 10f, -24f), new Vector3(-10f, 1f, -10f),
            "Sudut pandang dekat Player dan musuh yang mendekat"),
    };

    // Sudut khusus tembakan diisi saat runtime (lihat AmbilProyektil), karena posisinya
    // harus mengikuti jalur tembakan Player yang sebenarnya.
    static Sudut SudutTembakan = new Sudut("05_proyektil_terbang",
        new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f),
        "Proyektil sedang terbang ke arah musuh (ada jejak di belakangnya)");

    [MenuItem("Game/Ambil Screenshot Bukti", false, 31)]
    public static void Ambil()
    {
        langkah = 0;
        jumlahTersimpan = 0;

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        string dir = Path.Combine(Directory.GetCurrentDirectory(), FolderKeluar);
        Directory.CreateDirectory(dir);
        Debug.Log("[Screenshot] folder keluaran: " + dir);

        mulai = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
        langkah = 1;
        langkahMulai = EditorApplication.timeSinceStartup;
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup - mulai > 180d)
        {
            Debug.Log("[Screenshot] batas waktu tercapai.");
            Selesai();
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        // langkah 1: tunggu scene jalan (musuh mulai mengejar)
        if (langkah == 1)
        {
            if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup - langkahMulai > 3.0d)
            {
                langkah = 2;
                langkahMulai = EditorApplication.timeSinceStartup;
                Debug.Log("[Screenshot] Play mode jalan, mulai memotret.");
            }
            return;
        }

        // langkah 2..: potret tiap sudut dengan jeda supaya frame sempat render
        if (EditorApplication.timeSinceStartup - langkahMulai > 0.7d)
        {
            int idx = langkah - 2;
            if (idx < DaftarSudut.Length)
            {
                Potret(DaftarSudut[idx]);
                langkah++;
                langkahMulai = EditorApplication.timeSinceStartup;
            }
            else if (langkah == 2 + DaftarSudut.Length)
            {
                // --- khusus proyektil: tembak lalu bekukan di tengah jalur ---
                AmbilProyektil();
                langkah++;
                langkahMulai = EditorApplication.timeSinceStartup;
            }
            else
            {
                EditorApplication.isPlaying = false;
                langkah = 100;
                langkahMulai = EditorApplication.timeSinceStartup;
            }
            return;
        }

        if (langkah == 100 && !EditorApplication.isPlaying
            && EditorApplication.timeSinceStartup - langkahMulai > 0.8d)
            Selesai();
    }

    /// Menyiapkan adegan tembakan: satu musuh dipindah lurus di depan Player.
    static void SiapkanTembakanProyektil()
    {
        PlayerGerak gerak = UnityEngine.Object.FindObjectOfType<PlayerGerak>();
        PlayerTembak penembak = UnityEngine.Object.FindObjectOfType<PlayerTembak>();
        if (gerak == null || penembak == null || penembak.kamera == null) return;

        if (gerak.enabled) gerak.enabled = false;

        Camera kam = penembak.kamera;

        // Player diarahkan ke tengah arena: kamera orang ketiga ada ~6 m di belakang
        // Player, jadi arah pandang sembarang bisa membuat kamera keluar arena.
        Vector3 keTengah = Vector3.zero - gerak.transform.position;
        keTengah.y = 0f;
        if (keTengah.sqrMagnitude < 0.01f) keTengah = Vector3.forward;

        float yaw = Mathf.Atan2(keTengah.x, keTengah.z) * Mathf.Rad2Deg;
        gerak.ArahkanPandangan(yaw, 0f);
        kam.transform.localRotation = Quaternion.identity;

        // letakkan musuh tepat di depan kamera supaya ada sasaran yang jelas
        MusuhAI[] musuh = UnityEngine.Object.FindObjectsOfType<MusuhAI>();
        if (musuh.Length == 0) return;

        MusuhAI target = musuh[0];
        UnityEngine.AI.NavMeshAgent agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }

        Vector3 arahDepan = kam.transform.forward;
        target.transform.position = kam.transform.position + arahDepan * 14f;
        Physics.SyncTransforms();

        Debug.Log("[Screenshot] musuh uji '" + target.name + "' diletakkan 14 m di depan kamera untuk potret proyektil "
            + "(player diarahkan ke tengah arena, yaw " + FormatAngka.F(yaw, "F1") + " derajat).");
    }

    /// Menembak sekali lalu memotret proyektil di tengah jalur.
    /// Proyektil "dibekukan" di tengah karena kecepatan aslinya 95 m/s, sehingga
    /// sudah sampai dan hancur sebelum sempat dirender.
    static void AmbilProyektil()
    {
        PlayerTembak penembak = UnityEngine.Object.FindObjectOfType<PlayerTembak>();
        if (penembak == null || penembak.kamera == null) return;

        // 0) siapkan adegan: arahkan Player lurus, taruh musuh di depan sebagai sasaran
        SiapkanTembakanProyektil();

        // 1) tembak seperti biasa (sekaligus membuktikan proyektil benar-benar dibuat)
        penembak.Tembak();

        Proyektil[] daftar = UnityEngine.Object.FindObjectsOfType<Proyektil>();
        if (daftar.Length == 0)
        {
            Debug.LogWarning("[Screenshot] proyektil tidak ditemukan; potret proyektil dilewati.");
            return;
        }
        Proyektil proyektil = daftar[0];

        // 2) hitung titik tengah jalur: dari ujung laras, separuh jarak ke sasaran.
        //    (proyektil baru saja dibuat sehingga masih persis di ujung laras, jadi
        //     titik tengah dihitung dari arah tembakan, bukan dari posisinya sekarang)
        Camera kam = penembak.kamera;
        Vector3 asal = penembak.ujungLaras != null ? penembak.ujungLaras.position : kam.transform.position;
        float jarakTembakan = Mathf.Max(2f, penembak.JarakTembakanTerakhir);
        Vector3 arahTembakan = kam.transform.forward;
        Vector3 tengah = asal + arahTembakan * (jarakTembakan * 0.5f);

        proyektil.transform.position = tengah;

        // 3) gambar jejak dari ujung laras ke posisi proyektil (khusus untuk potret)
        GameObject goJejak = new GameObject("Jejak_Untuk_Potret");
        LineRenderer jejak = goJejak.AddComponent<LineRenderer>();
        jejak.positionCount = 2;
        jejak.useWorldSpace = true;
        jejak.startWidth = 0.10f;
        jejak.endWidth = 0.03f;
        jejak.numCapVertices = 4;

        Material mJejak = AsetVisual.MaterialJejak();
        if (mJejak != null) jejak.sharedMaterial = mJejak;

        jejak.startColor = new Color(1f, 0.62f, 0.15f, 0.95f);
        jejak.endColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        jejak.SetPosition(0, asal);
        jejak.SetPosition(1, tengah);

        // kamera potret diletakkan menyamping jalur tembakan supaya proyektil dan
        // jejaknya terlihat utuh dari sisi, berapa pun arah pandang Player
        Vector3 samping = Vector3.Cross(arahTembakan, Vector3.up).normalized;
        Vector3 posisiKamera = tengah + samping * 5.5f + Vector3.up * 2.4f;
        SudutTembakan = new Sudut("05_proyektil_terbang", posisiKamera, tengah,
            "Proyektil di tengah jalur tembakan, lengkap dengan jejak di belakangnya");

        Debug.Log("[Screenshot] proyektil dibekukan di tengah jalur: " + FormatAngka.V(tengah)
            + " (jarak dari laras " + FormatAngka.F(Vector3.Distance(asal, tengah), "F2")
            + " m dari total " + FormatAngka.F(jarakTembakan, "F2") + " m); kamera potret di "
            + FormatAngka.V(posisiKamera));

        Potret(SudutTembakan);

        UnityEngine.Object.DestroyImmediate(goJejak);
    }

    static void Potret(Sudut s)
    {
        Camera kam = Camera.main;
        if (kam == null)
        {
            Debug.LogWarning("[Screenshot] Camera.main tidak ada, sudut " + s.nama + " dilewati.");
            return;
        }

        // matikan sementara script penggerak player supaya rotasi pivot tidak menimpa
        // arah pandang kamera saat dipotret (player tidak bergerak tanpa input, jadi
        // posisi kamera tetap stabil selama pemotretan).
        PlayerGerak gerak = UnityEngine.Object.FindObjectOfType<PlayerGerak>();
        bool gerakAsli = gerak != null && gerak.enabled;
        if (gerak != null) gerak.enabled = false;

        Transform t = kam.transform;
        Vector3 posisiAsli = t.position;
        Quaternion rotasiAsli = t.rotation;

        t.position = s.posisi;
        t.rotation = Quaternion.LookRotation((s.lihatKe - s.posisi).normalized, Vector3.up);

        int lebar = 1280;
        int tinggi = 720;
        RenderTexture rt = new RenderTexture(lebar, tinggi, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;

        RenderTexture aktifSebelumnya = kam.targetTexture;
        kam.targetTexture = rt;
        kam.Render();
        kam.targetTexture = aktifSebelumnya;

        RenderTexture aktif = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D gambar = new Texture2D(lebar, tinggi, TextureFormat.RGB24, false);
        gambar.ReadPixels(new Rect(0, 0, lebar, tinggi), 0, 0);
        gambar.Apply();

        RenderTexture.active = aktif;
        rt.Release();

        byte[] png = gambar.EncodeToPNG();
        string namaFile = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), FolderKeluar), s.nama + ".png");
        File.WriteAllBytes(namaFile, png);
        UnityEngine.Object.DestroyImmediate(gambar);

        jumlahTersimpan++;
        Debug.Log("[Screenshot] tersimpan " + s.nama + ".png (" + png.Length + " byte) - " + s.keterangan);

        // kembalikan kamera ke posisi semula
        t.position = posisiAsli;
        t.rotation = rotasiAsli;
        if (gerak != null) gerak.enabled = gerakAsli;
    }

    static void Selesai()
    {
        EditorApplication.update -= Tick;
        EditorSettings.enterPlayModeOptionsEnabled = false;
        Debug.Log("[Screenshot] SELESAI: " + jumlahTersimpan + " screenshot tersimpan di " + FolderKeluar + "/");
        if (Application.isBatchMode) EditorApplication.Exit(jumlahTersimpan > 0 ? 0 : 1);
    }
}
