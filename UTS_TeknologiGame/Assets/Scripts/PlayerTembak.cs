using UnityEngine;

/// Menembak dengan raycast dari tengah layar. hit.distance dicetak ke Console dan
/// musuh yang kena HP-nya berkurang. Layer "Player" dikecualikan dari sinar.
public class PlayerTembak : MonoBehaviour
{
    [Header("Referensi kamera (titik & arah sinar)")]
    public Camera kamera;

    [Header("Setelan tembakan")]
    public float jarakMaks = 120f;
    public float damage = 25f;
    public float jedaTembak = 0.18f;

    [Header("Layer yang dilewati raycast")]
    [Tooltip("Layer 'Player' harus dikecualikan supaya badan Player tidak menahan tembakannya sendiri.")]
    public LayerMask maskTembakan = ~0;
    public Transform badanPlayer;

    [Header("Proyektil (tampilan tembakan)")]
    public bool pakaiProyektil = true;
    [Tooltip("Titik asal proyektil. Kalau kosong, dipakai posisi kamera.")]
    public Transform ujungLaras;
    public float kecepatanProyektil = 95f;
    public float ukuranProyektil = 0.16f;
    public Color warnaProyektil = new Color(1f, 0.85f, 0.3f);
    [Tooltip("Warna jejak yang tertinggal di belakang proyektil.")]
    public Color warnaJejak = new Color(1f, 0.62f, 0.15f);
    public bool jejakAktif = true;
    public float durasiJejak = 0.16f;
    public float lebarJejak = 0.09f;

    [Header("Efek kena")]
    public bool efekKenaAktif = true;
    public float durasiEfekKena = 0.18f;

    [Header("Tampilan layar")]
    [Tooltip("Garis bantu arah tembakan. Berguna untuk bukti laporan di Scene view.")]
    public bool gambarGarisBantu = false;
    public float durasiGarisBantu = 0.07f;
    public bool tampilkanCrosshair = true;

    [Header("Kontrol")]
    public KeyCode tombolTembak = KeyCode.Mouse0;    // klik kiri mouse

    float waktuTembakBerikutnya;
    LineRenderer garisBantu;
    float sisaGarisBantu;

    int jumlahTembakan;
    int jumlahKenaMusuh;
    int jumlahProyektilDiluncurkan;
    string infoTerakhir = "belum menembak";

    public int JumlahTembakan { get { return jumlahTembakan; } }
    public int JumlahKenaMusuh { get { return jumlahKenaMusuh; } }
    public int JumlahProyektilDiluncurkan { get { return jumlahProyektilDiluncurkan; } }
    public string InfoTerakhir { get { return infoTerakhir; } }
    public float JarakTembakanTerakhir { get; private set; }

    void Start()
    {
        if (kamera == null) kamera = Camera.main;

        if (badanPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) badanPlayer = p.transform;
        }

        // Pastikan layer Player benar-benar dilewati. Kalau layer-nya belum dibuat
        // (LayerMask.NameToLayer mengembalikan -1), mask dibiarkan apa adanya dan
        // pengecekan tambahan di Tembak() yang menanganinya.
        int layerPlayer = LayerMask.NameToLayer("Player");
        if (layerPlayer >= 0) maskTembakan = ~(1 << layerPlayer);

        SiapkanGarisBantu();
    }

    void SiapkanGarisBantu()
    {
        if (!gambarGarisBantu) return;

        GameObject go = new GameObject("Garis_Bantu_Tembakan");
        go.transform.SetParent(transform, false);

        garisBantu = go.AddComponent<LineRenderer>();
        garisBantu.positionCount = 2;
        garisBantu.startWidth = 0.04f;
        garisBantu.endWidth = 0.01f;
        garisBantu.useWorldSpace = true;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh != null) garisBantu.material = new Material(sh);

        garisBantu.startColor = new Color(1f, 0.92f, 0.25f, 0.7f);
        garisBantu.endColor = new Color(1f, 0.45f, 0.1f, 0f);
        garisBantu.enabled = false;
    }

    void Update()
    {
        if (kamera == null) kamera = Camera.main;

        if (garisBantu != null && sisaGarisBantu > 0f)
        {
            sisaGarisBantu -= Time.deltaTime;
            if (sisaGarisBantu <= 0f) garisBantu.enabled = false;
        }

        // ==== SOAL 2a: klik kiri mouse = menembak ====
        if (Input.GetKeyDown(tombolTembak) && Time.time >= waktuTembakBerikutnya)
            Tembak();
    }

    /// Menembakkan satu raycast. Dibuat public supaya bisa dipanggil dari tombol
    /// maupun dari verifikasi otomatis headless.
    public void Tembak()
    {
        if (kamera == null) return;

        waktuTembakBerikutnya = Time.time + jedaTembak;
        jumlahTembakan++;

        // Sinar keluar dari TENGAH layar (posisi crosshair) mengikuti arah kamera.
        // Saat dijalankan tanpa layar (batchmode, -nographics) ukuran layar bisa 0,
        // jadi dipakai sinar langsung dari titik & arah kamera - hasilnya identik.
        Ray ray;
        if (Screen.width > 0 && Screen.height > 0)
            ray = kamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        else
            ray = new Ray(kamera.transform.position, kamera.transform.forward);

        RaycastHit hit = new RaycastHit();
        bool kena = Physics.Raycast(ray, out hit, jarakMaks, maskTembakan, QueryTriggerInteraction.Ignore);

        // Jaring pengaman: kalau karena satu dan lain hal yang kena justru badan Player
        // sendiri (misal layer belum disiapkan), cari object berikutnya di belakangnya.
        if (kena && badanPlayer != null && hit.collider != null && hit.collider.transform.root == badanPlayer.root)
        {
            RaycastHit hitLanjut;
            if (RaycastLewatPlayer(ray, out hitLanjut)) { hit = hitLanjut; }
            else { kena = false; }
        }

        // titik kena: kalau tidak mengenai apa pun, dihitung dari ujung sinar
        Vector3 titikKena = kena ? hit.point : ray.origin + ray.direction * jarakMaks;

        if (kena)
        {
            JarakTembakanTerakhir = hit.distance;
            if (gambarGarisBantu) GambarGarisBantu(ray.origin, hit.point);

            MusuhAI musuh = hit.collider.GetComponentInParent<MusuhAI>();

            if (musuh != null)
            {
                jumlahKenaMusuh++;

                // ==== SOAL 2b: hit.distance dihitung lalu DICETAK ke Console ====
                // FormatAngka.F dipakai supaya pemisah desimalnya selalu TITIK
                // ("11.5 meter"), tidak ikut bahasa Windows (yang bisa jadi "11,5").
                Debug.Log("Kena Musuh pada jarak " + FormatAngka.F(hit.distance, "F1") + " meter");
                Debug.Log("[Game] detail tembakan -> musuh '" + musuh.name
                    + "' | jarak tepat " + FormatAngka.F(hit.distance, "F2") + " m"
                    + " | titik kena " + FormatAngka.V(hit.point)
                    + " | normal " + FormatAngka.V(hit.normal));

                infoTerakhir = "KENA MUSUH " + musuh.name + " @ " + FormatAngka.F(hit.distance, "F1") + " m";

                // ==== SOAL 2c: HP musuh berkurang; HP <= 0 -> musuh hancur ====
                musuh.TerimaDamage(damage);
            }
            else
            {
                Debug.Log("[Game] Raycast mengenai '" + hit.collider.name + "' pada jarak "
                    + FormatAngka.F(hit.distance, "F2") + " m (bukan musuh, HP tidak berkurang).");
                infoTerakhir = "kena " + hit.collider.name + " @ " + FormatAngka.F(hit.distance, "F1") + " m";
            }
        }
        else
        {
            JarakTembakanTerakhir = jarakMaks;
            if (gambarGarisBantu) GambarGarisBantu(ray.origin, titikKena);
            Debug.Log("[Game] Tembakan TIDAK mengenai apa pun (sinar habis di " + jarakMaks.ToString("F0") + " m).");
            infoTerakhir = "tidak kena (sinar habis di " + jarakMaks.ToString("F0") + " m)";
        }

        // Hasil sudah ditentukan raycast di atas. Proyektil di bawah ini hanya TAMPILAN,
        // supaya tembakan terlihat terbang ke titik kena.
        if (pakaiProyektil) LuncurkanProyektil(titikKena);
    }

    void LuncurkanProyektil(Vector3 titikKena)
    {
        Vector3 asal = ujungLaras != null ? ujungLaras.position : kamera.transform.position;

        GameObject go = AsetVisual.BuatObjekMenyala("Proyektil", warnaProyektil);
        go.transform.position = asal;
        go.transform.localScale = Vector3.one * Mathf.Max(0.02f, ukuranProyektil);

        Proyektil p = go.AddComponent<Proyektil>();
        p.Mulai(titikKena, kecepatanProyektil, jejakAktif, durasiJejak, lebarJejak,
            warnaJejak, efekKenaAktif, durasiEfekKena);

        jumlahProyektilDiluncurkan++;
    }

    /// Raycast ulang sambil membuang semua collider milik badan Player.
    bool RaycastLewatPlayer(Ray ray, out RaycastHit hasil)
    {
        hasil = new RaycastHit();
        RaycastHit[] semua = Physics.RaycastAll(ray, jarakMaks, maskTembakan, QueryTriggerInteraction.Ignore);

        float terdekat = float.MaxValue;
        bool ketemu = false;

        foreach (RaycastHit h in semua)
        {
            if (h.collider == null) continue;
            if (badanPlayer != null && h.collider.transform.root == badanPlayer.root) continue;
            if (h.distance < terdekat)
            {
                terdekat = h.distance;
                hasil = h;
                ketemu = true;
            }
        }
        return ketemu;
    }

    void GambarGarisBantu(Vector3 dari, Vector3 ke)
    {
        if (garisBantu == null) return;
        garisBantu.SetPosition(0, dari);
        garisBantu.SetPosition(1, ke);
        garisBantu.enabled = true;
        sisaGarisBantu = durasiGarisBantu;
    }

    void OnGUI()
    {
        // Yang tampil di layar Game hanya crosshair. Panel-panel info (daftar periksa,
        // statistik tembakan, dsb) sengaja dihilangkan supaya pandangan tidak terganggu.
        if (!tampilkanCrosshair) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        // crosshair dua lapis: inti putih di atas plat gelap 6 px, supaya tetap
        // terlihat di lantai terang (inti 2 px saja tidak cukup memisahkan)
        const float inti = 2f;      // ketebalan garis putih
        const float plat = 6f;      // ketebalan plat gelap (inti + 2 px di tiap sisi)
        const float panjang = 18f;  // panjang garis putih
        const float panjangPlat = panjang + (plat - inti);   // plat ikut memanjang

        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(cx - panjangPlat * 0.5f, cy - plat * 0.5f, panjangPlat, plat), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - plat * 0.5f, cy - panjangPlat * 0.5f, plat, panjangPlat), Texture2D.whiteTexture);

        GUI.color = new Color(1f, 1f, 1f, 0.95f);
        GUI.DrawTexture(new Rect(cx - panjang * 0.5f, cy - inti * 0.5f, panjang, inti), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - inti * 0.5f, cy - panjang * 0.5f, inti, panjang), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}
