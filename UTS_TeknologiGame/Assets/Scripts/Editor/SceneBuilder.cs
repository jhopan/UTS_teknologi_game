using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// Pembuat scene arena dari kode, diakhiri bake NavMesh.
/// Jalankan lewat menu Game > Build Scene Arena.
public static class SceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    const string SceneName = "Arena";

    // ukuran arena: setengah sisi = 20 m, jadi luas 40 x 40 m
    const float Half = 20f;

    // ================================================================== MENU
    [MenuItem("Game/Build Scene Arena (Arena + AI + Tembak)", false, 1)]
    public static void BuildAll()
    {
        PastikanLayerPlayer();
        BuatScene();
        BakeNavMesh();
        SetBuildSettings();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Teknologi Game",
                "Scene selesai dibuat:\n\n" +
                "  " + SceneFolder + "/" + SceneName + ".unity\n\n" +
                "NavMesh sudah di-BAKE dan tersimpan di dalam scene.\n\n" +
                "Tekan Play, lalu:\n" +
                "  WASD = jalan   Shift = lari   Space = lompat\n" +
                "  Mouse = lihat   Klik kiri = menembak\n" +
                "  B = cek NavMesh   H = sembunyikan HUD",
                "OK");
    }

    [MenuItem("Game/Bake NavMesh (ulang)", false, 11)]
    public static void BakeOnly()
    {
        BakeNavMesh();
        AssetDatabase.Refresh();
        Debug.Log("[Game] Bake NavMesh selesai. Simpan scene (Ctrl+S) supaya tersimpan.");
    }

    // ================================================================== LAYER
    /// Soal tidak menyebut layer, tapi layer "Player" dipakai supaya badan Player
    /// sendiri tidak menahan tembakan raycast-nya (kamera ada di belakang Player).
    static int PastikanLayerPlayer()
    {
        int ada = LayerMask.NameToLayer("Player");
        if (ada >= 0) return ada;

        Object[] aset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (aset == null || aset.Length == 0)
        {
            Debug.LogWarning("[Game] TagManager.asset tidak terbaca - layer Player dilewati.");
            return -1;
        }

        SerializedObject so = new SerializedObject(aset[0]);
        SerializedProperty layers = so.FindProperty("layers");
        if (layers == null) return -1;

        // indeks 0-7 dipakai Unity, layer bebas mulai dari 8
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = "Player";
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[Game] Layer 'Player' dibuat di indeks " + i + ".");
                return i;
            }
        }
        return -1;
    }

    // ================================================================== SCENE
    static void BuatScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- suasana ----------
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.43f, 0.48f);
        RenderSettings.fog = false;

        GameObject matahari = NewLight("Matahari", LightType.Directional,
            new Color(1f, 0.96f, 0.9f), 1.05f, new Vector3(48f, -38f, 0f));

        // ---------- 1a. LANTAI ARENA ----------
        GameObject arena = NewEmpty("Arena");

        GameObject lantai = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lantai.name = "Lantai_Arena";
        lantai.transform.SetParent(arena.transform, false);
        lantai.transform.position = Vector3.zero;
        lantai.transform.localScale = new Vector3(Half * 2f / 10f, 1f, Half * 2f / 10f);   // 40 x 40 m
        BeriWarna(lantai, new Color(0.30f, 0.33f, 0.36f));
        JadikanStatis(lantai);

        // ---------- 1a. RINTANGAN ----------
        GameObject rintangan = NewEmpty("Rintangan", arena.transform);

        // dinding pembatas luar (supaya musuh & player tidak keluar arena)
        NewKotak("Dinding_Utara", new Vector3(0f, 2f, Half + 0.3f), new Vector3(Half * 2f + 1.2f, 4f, 0.6f), rintangan.transform, new Color(0.45f, 0.40f, 0.36f));
        NewKotak("Dinding_Selatan", new Vector3(0f, 2f, -Half - 0.3f), new Vector3(Half * 2f + 1.2f, 4f, 0.6f), rintangan.transform, new Color(0.45f, 0.40f, 0.36f));
        NewKotak("Dinding_Timur", new Vector3(Half + 0.3f, 2f, 0f), new Vector3(0.6f, 4f, Half * 2f + 1.2f), rintangan.transform, new Color(0.45f, 0.40f, 0.36f));
        NewKotak("Dinding_Barat", new Vector3(-Half - 0.3f, 2f, 0f), new Vector3(0.6f, 4f, Half * 2f + 1.2f), rintangan.transform, new Color(0.45f, 0.40f, 0.36f));

        // rintangan dalam: sengaja dibuat berkelok supaya jalur musuh TIDAK lurus.
        // Kalau NavMesh benar-benar dipakai, musuh akan memutar lewat celah;
        // kalau hanya "gerak lurus ke player", musuh akan tersangkut di sini.
        NewKotak("Rintangan_A", new Vector3(-7f, 1.5f, 0f), new Vector3(14f, 3f, 0.8f), rintangan.transform, new Color(0.62f, 0.45f, 0.32f));
        NewKotak("Rintangan_B", new Vector3(7f, 1.5f, -7f), new Vector3(0.8f, 3f, 12f), rintangan.transform, new Color(0.62f, 0.45f, 0.32f));
        NewKotak("Rintangan_C", new Vector3(0f, 1.5f, 8f), new Vector3(20f, 3f, 0.8f), rintangan.transform, new Color(0.62f, 0.45f, 0.32f));
        NewKotak("Rintangan_D", new Vector3(12f, 1.5f, 6f), new Vector3(0.8f, 3f, 10f), rintangan.transform, new Color(0.62f, 0.45f, 0.32f));

        NewSilinder("Pilar_1", new Vector3(0f, 1.5f, -10f), new Vector3(1.6f, 1.5f, 1.6f), rintangan.transform, new Color(0.55f, 0.5f, 0.45f));
        NewSilinder("Pilar_2", new Vector3(-14f, 1.5f, -12f), new Vector3(1.6f, 1.5f, 1.6f), rintangan.transform, new Color(0.55f, 0.5f, 0.45f));
        NewSilinder("Pilar_3", new Vector3(15f, 1.5f, -14f), new Vector3(1.6f, 1.5f, 1.6f), rintangan.transform, new Color(0.55f, 0.5f, 0.45f));

        // ---------- PLAYER ----------
        Vector3 posisiPlayer = new Vector3(-15f, 1f, -15f);
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = posisiPlayer;

        // Capsule bawaan punya CapsuleCollider; CharacterController juga collider.
        // Dua collider sekaligus bikin tabrakan aneh, jadi CapsuleCollider dibuang.
        CapsuleCollider colliderBawaan = player.GetComponent<CapsuleCollider>();
        if (colliderBawaan != null) Object.DestroyImmediate(colliderBawaan);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero;
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.45f;

        int layerPlayer = LayerMask.NameToLayer("Player");
        if (layerPlayer >= 0) player.layer = layerPlayer;
        player.tag = "Player";

        BeriWarna(player, new Color(0.25f, 0.65f, 0.95f));

        PlayerGerak gerak = player.AddComponent<PlayerGerak>();

        // kamera anak: Player > KameraPivot > Main Camera (tampak orang ketiga)
        GameObject pivot = NewEmpty("KameraPivot", player.transform);
        pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        GameObject kameraObj = NewEmpty("Main Camera", pivot.transform);
        Camera kamera = kameraObj.AddComponent<Camera>();
        kameraObj.AddComponent<AudioListener>();
        kameraObj.tag = "MainCamera";
        kameraObj.transform.localPosition = new Vector3(0f, 0.35f, -6f);
        kameraObj.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
        kamera.fieldOfView = 65f;
        kamera.nearClipPlane = 0.05f;

        gerak.kameraPivot = pivot.transform;
        gerak.kamera = kameraObj.transform;

        PlayerTembak penembak = player.AddComponent<PlayerTembak>();
        penembak.kamera = kamera;
        penembak.badanPlayer = player.transform;
        penembak.jarakMaks = 120f;
        penembak.damage = 25f;
        if (layerPlayer >= 0) penembak.maskTembakan = ~(1 << layerPlayer);

        // Ujung laras: titik asal proyektil. Ditaruh sebagai anak KAMERA supaya
        // proyektil selalu muncul dari arah pandang, sedikit ke kanan-bawah seperti
        // senjata yang dipegang di sisi layar.
        GameObject ujungLaras = NewEmpty("UjungLaras", kameraObj.transform);
        ujungLaras.transform.localPosition = new Vector3(0.32f, -0.26f, 0.75f);
        penembak.ujungLaras = ujungLaras.transform;

        // ---------- 1c. MUSUH (NavMeshAgent) ----------
        GameObject musuhInduk = NewEmpty("Musuh");

        Vector3[] titikSpawn =
        {
            new Vector3( 15f, 1f,  15f),
            new Vector3(-15f, 1f,  15f),
            new Vector3( 15f, 1f, -15f),
            new Vector3(  5f, 1f, -15f)
        };

        List<MusuhAI> daftarMusuh = new List<MusuhAI>();

        for (int i = 0; i < titikSpawn.Length; i++)
        {
            GameObject musuh = BuatMusuh("Musuh_" + (i + 1), titikSpawn[i], player.transform, musuhInduk.transform);
            daftarMusuh.Add(musuh.GetComponent<MusuhAI>());
        }

        // ---------- HUD ----------
        GameObject hud = NewEmpty("HUD");
        GameHUD skripHud = hud.AddComponent<GameHUD>();
        skripHud.penembak = penembak;
        skripHud.gerakPlayer = gerak;

        // ---------- penanda ----------
        NewEmpty("Info_Scene").AddComponent<InfoPenanda>();

        // ---------- simpan ----------
        SimpanScene(SceneName);
        Debug.Log("[Game] Scene dibuat: " + titikSpawn.Length + " musuh, 1 player, "
            + (Half * 2f) + "x" + (Half * 2f) + " m arena. NavMesh dibake di langkah berikutnya.");
    }

    /// Musuh: Capsule + NavMeshAgent + script MusuhAI.
    static GameObject BuatMusuh(string nama, Vector3 posisi, Transform player, Transform induk)
    {
        GameObject musuh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        musuh.name = nama;
        musuh.transform.SetParent(induk, false);
        musuh.transform.position = posisi;
        BeriWarna(musuh, new Color(0.85f, 0.25f, 0.25f));

        NavMeshAgent agent = musuh.AddComponent<NavMeshAgent>();
        agent.agentTypeID = 0;            // 0 = "Humanoid" (radius 0,5 m; tinggi 2 m)
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.baseOffset = 1f;            // titik asal Capsule ada di tengah badan, bukan di kaki
        agent.speed = 3.5f;
        agent.acceleration = 14f;
        agent.angularSpeed = 260f;
        agent.stoppingDistance = 1.8f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        MusuhAI ai = musuh.AddComponent<MusuhAI>();
        ai.player = player;
        ai.hpMaks = 100f;
        ai.kecepatan = 3.5f;

        return musuh;
    }

    // ================================================================== BAKE
    /// Menghitung NavMesh dari geometri arena (soal 1b). Hasilnya tersimpan di file
    /// scene, jadi scene yang sudah di-bake tidak perlu bake ulang.
    static void BakeNavMesh()
    {
        Scene aktif = SceneManager.GetActiveScene();

        // PENTING: hanya geometri ARENA yang ditandai Navigation Static.
        // Player dan Musuh TIDAK boleh ditandai - kalau ikut di-bake, posisi awal
        // mereka akan dilubangi dari NavMesh dan musuh tidak bisa berjalan.
        GameObject arenaRoot = GameObject.Find("Arena");
        int ditandai = 0;
        if (arenaRoot != null)
            ditandai = TandaiStatisRekursif(arenaRoot);

        Debug.Log("[Game] " + ditandai + " objek arena ditandai Navigation Static untuk bake.");

        // ====== PROSES BAKE ======
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        // bukti terukur: hitung luas NavMesh yang dihasilkan
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        int titik = tri.vertices != null ? tri.vertices.Length : 0;
        int segitiga = tri.indices != null ? tri.indices.Length / 3 : 0;

        float luas = 0f;
        if (segitiga > 0)
        {
            for (int i = 0; i < tri.indices.Length; i += 3)
            {
                Vector3 a = tri.vertices[tri.indices[i]];
                Vector3 b = tri.vertices[tri.indices[i + 1]];
                Vector3 c = tri.vertices[tri.indices[i + 2]];
                luas += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            }
        }

        Debug.Log("[Game] BAKE NAVMESH SELESAI -> " + titik + " titik, " + segitiga
            + " segitiga, luas area walkable " + FormatAngka.F(luas, "F1") + " m2.");
        Debug.Log("[Game] Setelan agent: radius 0,5 m | tinggi 2 m | kemiringan maks 45 derajat | tinggi anak tangga 0,4 m.");

        EditorSceneManager.MarkSceneDirty(aktif);
        EditorSceneManager.SaveScene(aktif, SceneFolder + "/" + SceneName + ".unity");
        Debug.Log("[Game] Scene disimpan ulang bersama data NavMesh.");
    }

    static int TandaiStatisRekursif(GameObject go)
    {
        int jumlah = 0;

        // hanya geometri diam (punya Renderer, tanpa NavMeshAgent/CharacterController)
        if (go.GetComponent<Renderer>() != null
            && go.GetComponent<NavMeshAgent>() == null
            && go.GetComponent<CharacterController>() == null)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
            jumlah++;
        }

        foreach (Transform anak in go.transform)
            jumlah += TandaiStatisRekursif(anak.gameObject);

        return jumlah;
    }

    // ================================================================== HELPER
    static void SimpanScene(string namaFile)
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder))
        {
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene aktif = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(aktif);
        EditorSceneManager.SaveScene(aktif, SceneFolder + "/" + namaFile + ".unity");
        Debug.Log("[Game] scene tersimpan: " + SceneFolder + "/" + namaFile + ".unity");
    }

    static void SetBuildSettings()
    {
        string path = SceneFolder + "/" + SceneName + ".unity";
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        if (File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[Game] Build Settings diisi " + list.Count + " scene.");
    }

    static GameObject NewEmpty(string nama, Transform induk = null)
    {
        GameObject go = new GameObject(nama);
        if (induk != null) go.transform.SetParent(induk, false);
        return go;
    }

    static GameObject NewLight(string nama, LightType tipe, Color warna, float intensitas, Vector3 euler)
    {
        GameObject go = NewEmpty(nama);
        Light l = go.AddComponent<Light>();
        l.type = tipe;
        l.color = warna;
        l.intensity = intensitas;
        l.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(euler);
        return go;
    }

    static GameObject NewKotak(string nama, Vector3 posisi, Vector3 skala, Transform induk, Color warna)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nama;
        go.transform.SetParent(induk, false);
        go.transform.position = posisi;
        go.transform.localScale = skala;
        BeriWarna(go, warna);
        JadikanStatis(go);
        return go;
    }

    static GameObject NewSilinder(string nama, Vector3 posisi, Vector3 skala, Transform induk, Color warna)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nama;
        go.transform.SetParent(induk, false);
        go.transform.position = posisi;
        go.transform.localScale = skala;
        BeriWarna(go, warna);
        JadikanStatis(go);
        return go;
    }

    static void JadikanStatis(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
    }

    /// Memberi material berwarna sederhana supaya objek tidak semua abu-abu.
    static void BeriWarna(GameObject go, Color warna)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) return;

        Material m = new Material(sh);
        m.name = "Material_" + go.name;
        m.color = warna;
        r.sharedMaterial = m;
    }
}
