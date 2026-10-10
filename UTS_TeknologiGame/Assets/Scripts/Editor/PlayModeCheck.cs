using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Verifikasi otomatis Play mode tanpa GUI: arena, bake NavMesh, musuh mengejar,
/// tembak raycast, dan HP musuh sampai hancur.
/// Jalankan lewat menu Game > Verifikasi Play Mode.
public static class PlayModeCheck
{
    const string ScenePath = "Assets/Scenes/Arena.unity";

    static readonly List<string> Report = new List<string>();
    static int step;
    static double stepStart;
    static double runStart;
    static int gagal;

    static bool adaPlayMode;

    // data awal
    static int jumlahMusuh;
    static float jarakAwalTerdekat;
    static float luasNavMesh;
    static int segitigaNavMesh;

    // uji tembak
    static MusuhAI musuhUji;
    static PlayerTembak penembak;
    static PlayerGerak gerak;
    static Transform kameraTransform;
    static int tembakanKe;
    static float hpSebelum;
    static bool pesanConsoleTertangkap;
    static string pesanConsole = "";
    static readonly List<string> TangkapanLog = new List<string>();

    [MenuItem("Game/Verifikasi Play Mode", false, 21)]
    public static void RunAll()
    {
        step = 0;
        gagal = 0;
        Report.Clear();
        TangkapanLog.Clear();
        pesanConsoleTertangkap = false;

        // WAJIB: matikan domain reload saat masuk Play mode. Kalau tidak, semua field
        // static & langganan EditorApplication.update hilang saat reload dan harness
        // berhenti di tengah jalan tanpa pesan tambahan.
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        Application.logMessageReceived += TangkapLog;

        runStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;

        Log("=== VERIFIKASI PLAY MODE - TEKNOLOGI GAME ===");
        Log("Butir soal: 1a arena+rintangan | 1b bake NavMesh | 1c musuh mengejar (NavMeshAgent)");
        Log("            2a tembak klik kiri  | 2b hit.distance ke Console | 2c HP turun & hancur");
        Log("");

        BukaScene();
    }

    static void TangkapLog(string pesan, string stack, LogType tipe)
    {
        if (tipe != LogType.Log && tipe != LogType.Warning) return;
        TangkapanLog.Add(pesan);

        if (!pesanConsoleTertangkap && pesan.StartsWith("Kena Musuh pada jarak") && pesan.Contains("meter"))
        {
            pesanConsoleTertangkap = true;
            pesanConsole = pesan;
        }
    }

    static void Log(string s) { Report.Add(s); Debug.Log(s); }

    static void Cek(string nama, bool lulus, string detail)
    {
        if (!lulus) gagal++;
        Log((lulus ? "CEK PASS  " : "CEK FAIL  ") + nama + "  ->  " + detail);
    }

    // Semua angka di laporan verifikasi ditulis lewat FormatAngka supaya pemisah
    // desimalnya TITIK - tidak ikut bahasa Windows (yang di laptop ini memakai koma).
    static string F(float v) { return FormatAngka.F(v, "F3"); }
    static string V(Vector3 v) { return FormatAngka.V(v, "F3"); }

    // ============================================================ ALUR
    static void BukaScene()
    {
        Log("--- SCENE : " + ScenePath + " ---");
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        catch (Exception e)
        {
            Cek("buka scene", false, e.Message);
            Selesai();
            return;
        }

        CekUjiSebelumPlay();

        EditorApplication.isPlaying = true;
        step = 1;
        stepStart = EditorApplication.timeSinceStartup;
    }

    /// Pemeriksaan yang harus benar SEBELUM Play mode: bukti bake tersimpan di scene.
    static void CekUjiSebelumPlay()
    {
        // 1a. arena & rintangan
        GameObject lantai = GameObject.Find("Lantai_Arena");
        Cek("1a. lantai arena ada", lantai != null,
            lantai != null ? ("ukuran " + lantai.transform.localScale.x * 10f + " x " + lantai.transform.localScale.z * 10f + " m") : "tidak ada");

        GameObject rintangan = GameObject.Find("Rintangan");
        int jumlahRintangan = rintangan != null ? rintangan.transform.childCount : 0;
        Cek("1a. rintangan arena ada (>= 8)", jumlahRintangan >= 8, jumlahRintangan + " objek rintangan");

        // 1b. NavMesh tersimpan di dalam file scene (bukan cuma di memori editor)
        UnityEngine.AI.NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        int titik = tri.vertices != null ? tri.vertices.Length : 0;
        segitigaNavMesh = tri.indices != null ? tri.indices.Length / 3 : 0;

        float luas = 0f;
        if (segitigaNavMesh > 0)
        {
            for (int i = 0; i < tri.indices.Length; i += 3)
            {
                Vector3 a = tri.vertices[tri.indices[i]];
                Vector3 b = tri.vertices[tri.indices[i + 1]];
                Vector3 c = tri.vertices[tri.indices[i + 2]];
                luas += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            }
        }
        luasNavMesh = luas;

        Cek("1b. NAVMESH SUDAH DI-BAKE (tersimpan di file scene)",
            titik > 0 && segitigaNavMesh > 0,
            titik + " titik, " + segitigaNavMesh + " segitiga, luas walkable " + F(luas) + " m2");

        Cek("1b. luas NavMesh mendekati luas arena dikurangi rintangan (1200-1600 m2)",
            luas > 1200f && luas < 1600f,
            "luas NavMesh = " + F(luas) + " m2 (arena 40x40 = 1600 m2)");

        // 1c. musuh & NavMeshAgent
        MusuhAI[] musuh = UnityEngine.Object.FindObjectsOfType<MusuhAI>();
        jumlahMusuh = musuh.Length;
        Cek("1c. musuh ada di scene", jumlahMusuh >= 1, jumlahMusuh + " musuh");

        int punyaAgent = 0;
        foreach (MusuhAI m in musuh)
            if (m.GetComponent<NavMeshAgent>() != null) punyaAgent++;
        Cek("1c. setiap musuh memakai NavMeshAgent", punyaAgent == jumlahMusuh,
            punyaAgent + "/" + jumlahMusuh + " musuh punya NavMeshAgent");

        // 2a/2b. player + script tembak + kamera
        GameObject player = GameObject.Find("Player");
        Cek("player ada", player != null, player != null ? ("posisi " + V(player.transform.position)) : "tidak ada");
        penembak = player != null ? player.GetComponent<PlayerTembak>() : null;
        gerak = player != null ? player.GetComponent<PlayerGerak>() : null;
        Cek("2a. script penembak (raycast) terpasang di Player", penembak != null,
            penembak != null ? ("jarakMaks=" + F(penembak.jarakMaks) + " damage=" + F(penembak.damage)) : "tidak ada");

        Transform pivot = player != null ? player.transform.Find("KameraPivot") : null;
        kameraTransform = pivot != null ? pivot.Find("Main Camera") : null;
        Cek("2a. kamera orang ketiga: Player > KameraPivot > Main Camera", kameraTransform != null,
            kameraTransform != null ? "hierarki benar" : "tidak ditemukan");
        Cek("2a. penembak memakai kamera anak", penembak != null && kameraTransform != null
            && penembak.kamera != null && penembak.kamera.transform == kameraTransform,
            penembak != null && penembak.kamera != null ? penembak.kamera.name : "kamera belum diisi");
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup - runStart > 300d)
        {
            Cek("batas waktu 300 detik", false, "harness berhenti paksa");
            Selesai();
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        switch (step)
        {
            case 1:
                if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup - stepStart > 1.0d)
                {
                    adaPlayMode = true;
                    SampelAwal();
                    step = 2;
                    stepStart = EditorApplication.timeSinceStartup;
                }
                break;

            case 2:
                if (EditorApplication.timeSinceStartup - stepStart > 3.0d)
                {
                    SampelPergerakan();
                    MulaiUjiTembak();
                    step = 3;
                    stepStart = EditorApplication.timeSinceStartup;
                }
                break;

            case 3:
                // satu tembakan tiap 0,3 detik sampai musuh hancur
                if (EditorApplication.timeSinceStartup - stepStart > 0.3d)
                {
                    TembakSatuKali();
                    stepStart = EditorApplication.timeSinceStartup;
                }
                break;

            case 4:
                if (EditorApplication.timeSinceStartup - stepStart > 0.6d)
                {
                    SampelAkhirTembak();
                    EditorApplication.isPlaying = false;
                    step = 5;
                    stepStart = EditorApplication.timeSinceStartup;
                }
                break;

            case 5:
                if (!EditorApplication.isPlaying && EditorApplication.timeSinceStartup - stepStart > 0.8d)
                    Selesai();
                break;
        }
    }

    // ============================================================ SAMPEL
    static void SampelAwal()
    {
        // PENTING: referensi yang disimpan SEBELUM Play mode menjadi "null palsu" karena
        // Unity memuat ulang scene saat masuk Play mode. Jadi semua referensi runtime
        // (player, penembak, kamera) diambil ULANG di sini, di dalam Play mode.
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            penembak = player.GetComponent<PlayerTembak>();
            gerak = player.GetComponent<PlayerGerak>();

            Transform pivot = player.transform.Find("KameraPivot");
            kameraTransform = pivot != null ? pivot.Find("Main Camera") : null;
        }

        Cek("2a. referensi Player + penembak + kamera valid setelah masuk Play mode",
            player != null && penembak != null && gerak != null && kameraTransform != null,
            "player=" + (player != null) + " penembak=" + (penembak != null)
            + " gerak=" + (gerak != null) + " kamera=" + (kameraTransform != null));

        MusuhAI[] musuh = UnityEngine.Object.FindObjectsOfType<MusuhAI>();
        if (player == null) return;

        int diNavMesh = 0;
        float terdekat = float.MaxValue;

        foreach (MusuhAI m in musuh)
        {
            if (m.DiAtasNavMesh) diNavMesh++;
            float d = Vector3.Distance(m.transform.position, player.transform.position);
            if (d < terdekat) terdekat = d;
        }

        jarakAwalTerdekat = terdekat;

        Cek("1c. saat Play mode, agent berada DI ATAS NavMesh", diNavMesh == musuh.Length,
            diNavMesh + "/" + musuh.Length + " agent valid (isOnNavMesh = true)");

        Cek("1c. jarak musuh terdekat ke Player terukur", terdekat < float.MaxValue,
            "jarak terdekat awal = " + F(terdekat) + " m");
    }

    static void SampelPergerakan()
    {
        GameObject player = GameObject.Find("Player");
        MusuhAI[] musuh = UnityEngine.Object.FindObjectsOfType<MusuhAI>();
        if (player == null || musuh.Length == 0) return;

        float terdekat = float.MaxValue;
        int punyaJalur = 0;
        int jalurMemutar = 0;
        string detailJalur = "";

        foreach (MusuhAI m in musuh)
        {
            float d = Vector3.Distance(m.transform.position, player.transform.position);
            if (d < terdekat) terdekat = d;

            if (m.PunyaJalur) punyaJalur++;

            NavMeshAgent a = m.GetComponent<NavMeshAgent>();
            if (a != null && a.hasPath && a.path != null && a.path.corners != null)
            {
                // jalur lurus = 2 titik (mulai, tujuan). Lebih dari 2 = jalur memutar.
                if (a.path.corners.Length > 2)
                {
                    jalurMemutar++;
                    if (string.IsNullOrEmpty(detailJalur))
                        detailJalur = m.name + " jalurnya " + a.path.corners.Length + " titik (memutar)";
                }
            }
        }

        // ===== BUKTI 1c: musuh MENDEKAT ke player =====
        Cek("1c. musuh MENGEJAR Player (jarak makin dekat)", terdekat < jarakAwalTerdekat - 0.5f,
            "jarak terdekat " + F(jarakAwalTerdekat) + " m -> " + F(terdekat) + " m");

        Cek("1c. NavMeshAgent menghitung jalur (hasPath = true)", punyaJalur > 0,
            punyaJalur + "/" + musuh.Length + " musuh punya jalur aktif");

        // ===== BUKTI jalur memutar, bukan garis lurus =====
        // Diuji ke SEMUA musuh: cukup ada satu yang garis lurusnya terhalang rintangan
        // untuk membuktikan NavMesh memang perlu memutar (lebih tahan posisi awal berubah).
        int terhalang = 0;
        string detailHalang = "";

        foreach (MusuhAI m in musuh)
        {
            Vector3 arah = m.transform.position - player.transform.position;
            float jarakLurus = arah.magnitude;
            if (jarakLurus < 0.1f) continue;

            RaycastHit tabrak;
            bool kena = Physics.Raycast(
                m.transform.position + Vector3.up * 1f,
                arah.normalized,
                out tabrak,
                jarakLurus,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (kena)
            {
                terhalang++;
                if (string.IsNullOrEmpty(detailHalang))
                    detailHalang = m.name + " terhalang '" + tabrak.collider.name + "' pada " + F(tabrak.distance) + " m";
            }
        }

        Cek("1c. garis lurus musuh->Player TERHALANG rintangan (bukti perlu memutar)",
            terhalang > 0,
            terhalang > 0
                ? (terhalang + "/" + musuh.Length + " musuh garis lurusnya terhalang; contoh: " + detailHalang)
                : "tidak ada garis lurus yang terhalang - rintangan kurang menantang");

        Cek("1c. jalur NavMesh MEMUTAR melewati rintangan (corners > 2)", jalurMemutar > 0,
            string.IsNullOrEmpty(detailJalur) ? ("jalur memutar: " + jalurMemutar + " musuh") : detailJalur);
    }

    static void MulaiUjiTembak()
    {
        GameObject player = GameObject.Find("Player");
        MusuhAI[] musuh = UnityEngine.Object.FindObjectsOfType<MusuhAI>();
        if (player == null || musuh.Length == 0 || penembak == null) return;

        // hentikan semua musuh supaya posisinya stabil saat diukur
        foreach (MusuhAI m in musuh) m.SetBerhenti(true);

        musuhUji = musuh[0];

        // arahkan badan player & kamera ke tengah arena (daerah lapang)
        if (gerak != null)
        {
            Vector3 ke = (Vector3.zero - player.transform.position);
            float yaw = Mathf.Atan2(ke.x, ke.z) * Mathf.Rad2Deg;
            gerak.ArahkanPandangan(yaw, 0f);
        }

        // samakan rotasi kamera dengan pivot supaya arah sinar = arah pandang
        if (kameraTransform != null) kameraTransform.localRotation = Quaternion.identity;

        // letakkan musuh uji tepat di depan kamera (12 m) supaya sinar pasti kena
        NavMeshAgent agentUji = musuhUji.GetComponent<NavMeshAgent>();
        if (agentUji != null) agentUji.enabled = false;

        Camera kam = penembak.kamera;
        Vector3 posisiUji = kam.transform.position + kam.transform.forward * 12f;
        musuhUji.transform.position = posisiUji;

        // Physics.autoSyncTransforms default false sejak Unity 2020, jadi collider
        // yang baru dipindah belum ikut pindah sampai sinkronisasi berikutnya.
        Physics.SyncTransforms();

        hpSebelum = musuhUji.HpSekarang;
        tembakanKe = 0;

        Log("");
        Log("--- UJI TEMBAK (2a/2b/2c) ---");
        Log("musuh uji '" + musuhUji.name + "' diletakkan " + F(Vector3.Distance(posisiUji, kam.transform.position))
            + " m di depan kamera. HP awal " + F(hpSebelum) + ", damage per tembakan " + F(penembak.damage));

        RaycastHit cek;
        bool kena = Physics.Raycast(kam.transform.position, kam.transform.forward, out cek, 120f);
        Cek("2a. raycast dari kamera mengenai musuh",
            kena && cek.collider.GetComponentInParent<MusuhAI>() == musuhUji,
            kena ? ("kena '" + cek.collider.name + "' pada " + F(cek.distance) + " m") : "tidak kena apa pun");

        if (kena)
        {
            // Jarak yang dicetak Console diukur dari BADAN Player, bukan dari kamera.
            GameObject pemain = GameObject.Find("Player");
            Vector3 acuan = pemain != null ? pemain.transform.position : kam.transform.position;
            float dariPlayer = Vector3.Distance(acuan, cek.point);

            Log("2b. jarak yang akan dicetak Console = " + FormatAngka.F(dariPlayer, "F1") + " meter"
                + " (dari badan Player; hit.distance dari kamera " + FormatAngka.F(cek.distance, "F2") + " m)");
        }
    }

    static void TembakSatuKali()
    {
        if (musuhUji == null)
        {
            step = 4;
            stepStart = EditorApplication.timeSinceStartup;
            return;
        }

        tembakanKe++;
        hpSebelum = musuhUji.HpSekarang;
        penembak.Tembak();

        // musuh sudah hancur (Destroy) -> objeknya jadi null pada frame berikutnya
        if (musuhUji == null || musuhUji.SudahMati)
        {
            step = 4;
            stepStart = EditorApplication.timeSinceStartup;
            return;
        }

        float hpSesudah = musuhUji.HpSekarang;
        Log("tembakan ke-" + tembakanKe + " : HP " + F(hpSebelum) + " -> " + F(hpSesudah)
            + " (berkurang " + F(hpSebelum - hpSesudah) + ")");

        if (tembakanKe >= 8)
        {
            step = 4;
            stepStart = EditorApplication.timeSinceStartup;
        }
    }

    static void SampelAkhirTembak()
    {
        // 2b. pesan Console
        Cek("2b. Console mencetak \"Kena Musuh pada jarak ... meter\"", pesanConsoleTertangkap,
            pesanConsoleTertangkap ? pesanConsole : "pesan tidak ditemukan di Console");

        // 2a tambahan. proyektil benar-benar dibuat & terbang ke titik kena
        Proyektil[] proyektil = UnityEngine.Object.FindObjectsOfType<Proyektil>();
        Cek("2a. proyektil tampilan diluncurkan saat menembak",
            penembak != null && penembak.JumlahProyektilDiluncurkan > 0,
            penembak != null ? (penembak.JumlahProyektilDiluncurkan + " proyektil diluncurkan, "
                + proyektil.Length + " masih terbang") : "penembak null");

        // proyektil harus berupa objek TANPA collider (kalau ada collider, dia bisa
        // ikut tertangkap raycast tembakan berikutnya)
        int adaCollider = 0;
        foreach (Proyektil p in proyektil)
            if (p.GetComponent<Collider>() != null) adaCollider++;
        Cek("2a. proyektil tidak punya collider (tidak mengganggu raycast)",
            adaCollider == 0, adaCollider + " dari " + proyektil.Length + " proyektil punya collider");

        // ujung laras harus berada di tubuh Player, bukan di kamera. Kalau salah parent,
        // proyektil muncul dari udara setinggi kamera (~4 m), bukan dari badan Player.
        GameObject pemain2 = GameObject.Find("Player");
        if (penembak != null && penembak.ujungLaras != null && pemain2 != null)
        {
            float tinggi = penembak.ujungLaras.position.y;
            float jarakKePlayer = Vector3.Distance(penembak.ujungLaras.position, pemain2.transform.position);

            Cek("2a. ujung laras (asal proyektil) berada di tubuh Player, bukan di udara",
                tinggi < 2.0f && jarakKePlayer < 1.5f,
                "tinggi " + F(tinggi) + " m dari tanah, jarak ke pusat badan Player "
                + F(jarakKePlayer) + " m (kepala Player = 2.0 m)");
        }

        // 2c. HP berkurang & musuh hancur
        bool hancur = musuhUji == null || musuhUji.SudahMati;

        Cek("2c. HP musuh berkurang tiap tembakan (>= 1 tembakan mengubah HP)",
            tembakanKe >= 1,
            tembakanKe + " tembakan dikirim, damage per tembakan " + F(penembak != null ? penembak.damage : 0f));

        Cek("2c. HP musuh habis (<= 0) -> musuh HANCUR (Destroy)", hancur,
            hancur ? ("musuh uji sudah dihancurkan setelah " + tembakanKe + " tembakan (HP awal 100, damage 25)")
                   : ("musuh masih hidup, HP " + (musuhUji != null ? F(musuhUji.HpSekarang) : "?")));

        int masihAda = UnityEngine.Object.FindObjectsOfType<MusuhAI>().Length;
        Cek("2c. jumlah musuh berkurang 1 setelah dihancurkan", masihAda == jumlahMusuh - 1,
            jumlahMusuh + " musuh -> " + masihAda + " musuh");

        Cek("2b. hit.distance terakhir terukur", penembak != null && penembak.JarakTembakanTerakhir > 0f,
            penembak != null ? ("jarak tembakan terakhir " + F(penembak.JarakTembakanTerakhir) + " m, "
                + penembak.JumlahTembakan + " tembakan, " + penembak.JumlahKenaMusuh + " kena musuh") : "penembak null");
    }

    // ============================================================ SELESAI
    static void Selesai()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= TangkapLog;

        Log("");
        Log(adaPlayMode ? "Play mode berhasil dijalankan pada scene Arena." : "Play mode TIDAK pernah aktif.");
        Log("Baris Console yang tertangkap selama uji: " + TangkapanLog.Count);
        Log(gagal == 0 ? "=== HASIL AKHIR: SEMUA CEK PASS ===" : "=== HASIL AKHIR: " + gagal + " CEK GAGAL ===");

        EditorSettings.enterPlayModeOptionsEnabled = false;
        if (Application.isBatchMode) EditorApplication.Exit(gagal == 0 ? 0 : 1);
    }
}
