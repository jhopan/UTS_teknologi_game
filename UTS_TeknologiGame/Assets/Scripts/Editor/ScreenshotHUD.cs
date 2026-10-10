using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Pengambil screenshot yang ikut menangkap OnGUI (crosshair, bar darah).
/// Berbeda dari ScreenshotBukti yang memakai Camera.Render() dan melewatkan OnGUI.
public static class ScreenshotHUD
{
    const string ScenePath = "Assets/Scenes/Arena.unity";
    const string FolderKeluar = "Bukti_Screenshot";

    static int langkah;
    static double langkahMulai;
    static double mulai;
    static int jumlahTersimpan;

    [MenuItem("Game/Ambil Screenshot HUD", false, 32)]
    public static void Ambil()
    {
        // ScreenCapture butuh backbuffer layar nyata. Di batchmode Unity memakai
        // display dummy tanpa backbuffer, jadi OnGUI tidak bisa ditangkap dan
        // file PNG tidak akan tertulis. Lebih baik berhenti di sini dengan pesan
        // jelas daripada menjalankan proses yang pasti gagal.
        if (Application.isBatchMode)
        {
            Debug.LogWarning("[HUD-SS] Screenshot HUD tidak bisa diambil di batchmode. "
                + "Jalankan lewat Editor GUI: menu Game > Ambil Screenshot HUD.");
            EditorApplication.Exit(1);
            return;
        }

        langkah = 0;
        jumlahTersimpan = 0;

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        string dir = Path.Combine(Directory.GetCurrentDirectory(), FolderKeluar);
        Directory.CreateDirectory(dir);

        // diagnostik: batchmode kadang tidak punya permukaan layar nyata
        Debug.Log("[HUD-SS] ukuran layar terdeteksi: " + Screen.width + " x " + Screen.height
            + " | batchmode=" + Application.isBatchMode);

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
            Debug.Log("[HUD-SS] batas waktu tercapai.");
            Selesai();
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (langkah == 1)
        {
            if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup - langkahMulai > 3.0d)
            {
                SiapkanAdegan();
                langkah = 2;
                langkahMulai = EditorApplication.timeSinceStartup;
            }
            return;
        }

        if (langkah == 2 && EditorApplication.timeSinceStartup - langkahMulai > 0.5d)
        {
            Potret("10_hud_crosshair");
            langkah = 3;
            langkahMulai = EditorApplication.timeSinceStartup;
            return;
        }

        if (langkah == 3 && EditorApplication.timeSinceStartup - langkahMulai > 1.5d)
        {
            // tembak 2x supaya bar darah musuh terlihat berkurang
            TembakDua();
            langkah = 4;
            langkahMulai = EditorApplication.timeSinceStartup;
            return;
        }

        if (langkah == 4 && EditorApplication.timeSinceStartup - langkahMulai > 0.6d)
        {
            Potret("11_hud_bar_darah");
            langkah = 5;
            langkahMulai = EditorApplication.timeSinceStartup;
            return;
        }

        // ScreenCapture menulis file di akhir frame, jadi beri jeda lebih panjang
        // sebelum keluar dari Play mode supaya penulisan file sempat selesai.
        if (langkah == 5 && EditorApplication.timeSinceStartup - langkahMulai > 2.5d)
        {
            EditorApplication.isPlaying = false;
            langkah = 100;
            langkahMulai = EditorApplication.timeSinceStartup;
            return;
        }

        if (langkah == 100 && !EditorApplication.isPlaying
            && EditorApplication.timeSinceStartup - langkahMulai > 2.0d)
            Selesai();
    }

    /// Player diarahkan ke tengah arena, satu musuh ditaruh di depan supaya
    /// crosshair, bar darah, dan proyektil semuanya masuk frame.
    static void SiapkanAdegan()
    {
        PlayerGerak gerak = UnityEngine.Object.FindObjectOfType<PlayerGerak>();
        PlayerTembak penembak = UnityEngine.Object.FindObjectOfType<PlayerTembak>();
        if (gerak == null || penembak == null || penembak.kamera == null) return;

        if (gerak.enabled) gerak.enabled = false;

        Vector3 keTengah = Vector3.zero - gerak.transform.position;
        keTengah.y = 0f;
        if (keTengah.sqrMagnitude < 0.01f) keTengah = Vector3.forward;

        float yaw = Mathf.Atan2(keTengah.x, keTengah.z) * Mathf.Rad2Deg;
        gerak.ArahkanPandangan(yaw, 0f);
        penembak.kamera.transform.localRotation = Quaternion.identity;

        MusuhAI[] musuh = UnityEngine.Object.FindObjectsOfType<MusuhAI>();
        if (musuh.Length == 0) return;

        MusuhAI target = musuh[0];
        NavMeshAgentLokal(target);
        target.transform.position = penembak.kamera.transform.position
            + penembak.kamera.transform.forward * 9f;
        Physics.SyncTransforms();

        Debug.Log("[HUD-SS] musuh '" + target.name + "' ditaruh 9 m di depan kamera untuk potret HUD.");
    }

    static void NavMeshAgentLokal(MusuhAI m)
    {
        UnityEngine.AI.NavMeshAgent a = m.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (a != null) { a.isStopped = true; a.enabled = false; }
    }

    static void TembakDua()
    {
        PlayerTembak penembak = UnityEngine.Object.FindObjectOfType<PlayerTembak>();
        if (penembak == null) return;
        penembak.Tembak();
        penembak.Tembak();
        Debug.Log("[HUD-SS] 2 tembakan dikirim supaya bar darah musuh terlihat turun.");
    }

    /// Memotret frame akhir (termasuk OnGUI) memakai ScreenCapture.
    static void Potret(string nama)
    {
        string path = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), FolderKeluar), nama + ".png");

        if (File.Exists(path)) File.Delete(path);

        ScreenCapture.CaptureScreenshot(path);
        jumlahTersimpan++;
        Debug.Log("[HUD-SS] permintaan potret: " + nama + ".png (tersimpan di akhir frame)");
    }

    static void Selesai()
    {
        EditorApplication.update -= Tick;
        EditorSettings.enterPlayModeOptionsEnabled = false;

        // laporkan file yang benar-benar ada di disk, bukan yang cuma diminta
        string dir = Path.Combine(Directory.GetCurrentDirectory(), FolderKeluar);
        int ada = 0;
        foreach (string f in Directory.GetFiles(dir, "1*.png"))
        {
            Debug.Log("[HUD-SS] file ada di disk: " + Path.GetFileName(f) + " (" + new FileInfo(f).Length + " byte)");
            ada++;
        }

        Debug.Log("[HUD-SS] SELESAI: diminta " + jumlahTersimpan + ", ada di disk " + ada);
        if (Application.isBatchMode) EditorApplication.Exit(ada > 0 ? 0 : 1);
    }
}
