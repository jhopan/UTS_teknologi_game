using UnityEngine;
using UnityEngine.AI;

/// Musuh AI yang mengejar Player memakai NavMeshAgent; script hanya memberi target.
/// HP dikurangi oleh PlayerTembak, dan HP <= 0 musuh dihancurkan.
public class MusuhAI : MonoBehaviour
{
    [Header("Target yang dikejar")]
    public Transform player;

    [Header("Darah / HP")]
    public float hpMaks = 100f;
    public float hpSekarang = 100f;

    [Header("Setelan NavMeshAgent")]
    public float kecepatan = 3.5f;
    public float percepatan = 14f;
    public float kecepatanPutar = 260f;
    public float jarakBerhenti = 1.8f;

    [Header("Efek visual")]
    public float durasiKilat = 0.12f;
    public bool tampilkanBarDarah = true;

    NavMeshAgent agent;
    Renderer[] semuaRenderer;
    Material[] materialInstance;
    Color[] warnaAsli;

    float waktuKilat;
    float jarakKePlayer = -1f;
    bool sudahMati;

    // --- dibaca HUD & verifikasi otomatis ---
    public float HpSekarang { get { return hpSekarang; } }
    public float HpMaks { get { return hpMaks; } }
    public float JarakKePlayer { get { return jarakKePlayer; } }
    public bool SudahMati { get { return sudahMati; } }
    public bool DiAtasNavMesh { get { return agent != null && agent.enabled && agent.isOnNavMesh; } }
    public bool PunyaJalur { get { return DiAtasNavMesh && agent.hasPath; } }
    public float KecepatanAgent { get { return agent != null ? agent.speed : 0f; } }
    public Vector3 KecepatanSekarang { get { return agent != null ? agent.velocity : Vector3.zero; } }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        semuaRenderer = GetComponentsInChildren<Renderer>();
        materialInstance = new Material[semuaRenderer.Length];
        warnaAsli = new Color[semuaRenderer.Length];

        for (int i = 0; i < semuaRenderer.Length; i++)
        {
            if (semuaRenderer[i] == null) continue;
            materialInstance[i] = semuaRenderer[i].material;   // instance, aman diubah-ubah
            warnaAsli[i] = materialInstance[i].color;
        }
    }

    void Start()
    {
        CariPlayer();
        hpSekarang = hpMaks;

        if (agent != null)
        {
            agent.speed = kecepatan;
            agent.acceleration = percepatan;
            agent.angularSpeed = kecepatanPutar;
            agent.stoppingDistance = jarakBerhenti;
            agent.autoBraking = true;
            agent.updateRotation = true;
            agent.updatePosition = true;
        }
    }

    void CariPlayer()
    {
        if (player != null) return;

        GameObject cari = GameObject.FindGameObjectWithTag("Player");
        if (cari == null) cari = GameObject.Find("Player");
        if (cari != null) player = cari.transform;
    }

    void Update()
    {
        if (sudahMati) return;

        if (player == null)
        {
            CariPlayer();
            return;
        }

        jarakKePlayer = Vector3.Distance(transform.position, player.position);

        // musuh cukup diberi titik tujuan; jalur & penghindaran dikerjakan agent
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(player.position);

        KilatKenaTembakan();
    }

    void KilatKenaTembakan()
    {
        if (waktuKilat <= 0f) return;

        waktuKilat -= Time.deltaTime;
        float t = Mathf.Clamp01(waktuKilat / Mathf.Max(durasiKilat, 0.0001f));

        for (int i = 0; i < materialInstance.Length; i++)
            if (materialInstance[i] != null)
                materialInstance[i].color = Color.Lerp(warnaAsli[i], Color.red, t);
    }

    /// Menghentikan musuh sementara (dipakai peragaan & verifikasi otomatis).
    public void SetBerhenti(bool berhenti)
    {
        if (agent == null) return;
        agent.isStopped = berhenti;
        if (berhenti) agent.velocity = Vector3.zero;
    }

    /// Dipanggil PlayerTembak saat raycast mengenai musuh ini.
    public void TerimaDamage(float damage)
    {
        if (sudahMati) return;

        hpSekarang -= damage;
        waktuKilat = durasiKilat;

        Debug.Log("[Game] Musuh '" + name + "' kena " + damage.ToString("F0")
            + " damage -> HP " + Mathf.Max(hpSekarang, 0f).ToString("F0") + " / " + hpMaks.ToString("F0"));

        if (hpSekarang <= 0f) Hancur();
    }

    void Hancur()
    {
        sudahMati = true;

        if (agent != null) agent.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        Debug.Log("[Game] HP musuh '" + name + "' habis (<= 0) -> MUSUH HANCUR (Destroy).");
        Destroy(gameObject);
    }

    /// Bar darah di atas kepala musuh. Warna bar 3 tingkat diskret, dan label HP
    /// diberi plat gelap supaya kontrasnya tetap memenuhi WCAG AA di lantai terang.
    void OnGUI()
    {
        if (!tampilkanBarDarah || sudahMati) return;

        Camera kam = Camera.main;
        if (kam == null) return;

        Vector3 titikLayar = kam.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
        if (titikLayar.z <= 0f) return;                    // musuh di belakang kamera

        float lebar = 70f;
        float tinggi = 9f;
        float x = titikLayar.x - lebar * 0.5f;
        float y = Screen.height - titikLayar.y - tinggi;

        float persen = hpMaks > 0f ? Mathf.Clamp01(hpSekarang / hpMaks) : 0f;

        // plat gelap di belakang bar supaya kontrasnya tidak bergantung warna lantai
        GUI.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, lebar + 4f, tinggi + 4f), Texture2D.whiteTexture);

        // isi bar: 3 warna diskret
        GUI.color = WarnaBar(persen);
        GUI.DrawTexture(new Rect(x, y, lebar * persen, tinggi), Texture2D.whiteTexture);

        GUI.color = Color.white;

        // label HP, juga dengan plat gelap di belakangnya
        string label = "HP " + Mathf.Max(hpSekarang, 0f).ToString("F0") + " / " + hpMaks.ToString("F0");

        float lebarLabel = 96f;
        float tinggiLabel = 17f;
        float xLabel = titikLayar.x - lebarLabel * 0.5f;
        float yLabel = y - tinggiLabel - 3f;

        GUI.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
        GUI.DrawTexture(new Rect(xLabel, yLabel, lebarLabel, tinggiLabel), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle gayaLabel = new GUIStyle(GUI.skin.label);
        gayaLabel.alignment = TextAnchor.MiddleCenter;
        gayaLabel.fontSize = 11;
        gayaLabel.normal.textColor = Color.white;
        GUI.Label(new Rect(xLabel, yLabel, lebarLabel, tinggiLabel), label, gayaLabel);
    }

    /// Warna isi bar darah menurut sisa HP.
    /// Ambang dipilih supaya perubahan warna terlihat sebagai tanda bahaya yang jelas,
    /// bukan gradasi halus yang tidak terbaca saat bermain cepat.
    static Color WarnaBar(float persen)
    {
        if (persen > 0.6f) return new Color(0.20f, 0.75f, 0.25f);   // hijau: aman
        if (persen > 0.3f) return new Color(0.95f, 0.70f, 0.10f);   // kuning: waspada
        return new Color(0.85f, 0.20f, 0.20f);                       // merah: kritis
    }
}
