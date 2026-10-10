using UnityEngine;

/// Penggerak Player: WASD jalan, Shift lari, Space lompat, mouse memutar pandangan.
/// Player > KameraPivot > Main Camera, jadi kamera mengikuti Player (orang ketiga).
/// CharacterController dipakai supaya Player tidak menembus dinding rintangan.
[RequireComponent(typeof(CharacterController))]
public class PlayerGerak : MonoBehaviour
{
    [Header("Gerak")]
    public float kecepatanJalan = 6f;
    public float kecepatanLari = 9.5f;
    public float tinggiLompat = 1.5f;
    public float gravitasi = -19.62f;

    [Header("Mouse look")]
    public float sensitivitas = 3f;
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Referensi kamera (anak dari Player)")]
    public Transform kameraPivot;
    public Transform kamera;

    [Header("Kursor")]
    public KeyCode tombolKunciKursor = KeyCode.Tab;

    CharacterController cc;
    float kecepatanVertikal;
    float yaw;
    float pitch = 12f;

    public float Yaw { get { return yaw; } }
    public float Pitch { get { return pitch; } }
    public bool DiTanah { get { return cc != null && cc.isGrounded; } }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        yaw = transform.eulerAngles.y;

        if (kameraPivot == null && transform.childCount > 0)
            kameraPivot = transform.GetChild(0);

        if (kamera == null && kameraPivot != null && kameraPivot.childCount > 0)
            kamera = kameraPivot.GetChild(0);

        KunciKursor(true);
    }

    void Update()
    {
        // Tab = kunci / lepas kunci kursor (supaya mouse nyaman dipakai memutar pandangan)
        if (Input.GetKeyDown(tombolKunciKursor))
            KunciKursor(Cursor.lockState != CursorLockMode.Locked);

        // ---- putar BADAN player memakai mouse X ----
        yaw += Input.GetAxis("Mouse X") * sensitivitas;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ---- putar KAMERA naik-turun memakai mouse Y ----
        pitch -= Input.GetAxis("Mouse Y") * sensitivitas;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (kameraPivot != null)
            kameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // ---- WASD relatif terhadap arah pandang ----
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 arah = transform.right * h + transform.forward * v;
        if (arah.sqrMagnitude > 1f) arah.Normalize();

        float kecepatan = Input.GetKey(KeyCode.LeftShift) ? kecepatanLari : kecepatanJalan;

        // ---- gravitasi & lompat ----
        if (cc.isGrounded && kecepatanVertikal < 0f) kecepatanVertikal = -2f;
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space))
            kecepatanVertikal = Mathf.Sqrt(tinggiLompat * -2f * gravitasi);
        kecepatanVertikal += gravitasi * Time.deltaTime;

        Vector3 gerak = arah * kecepatan + Vector3.up * kecepatanVertikal;
        cc.Move(gerak * Time.deltaTime);
    }

    void KunciKursor(bool kunci)
    {
        Cursor.lockState = kunci ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !kunci;
    }

    /// Dipakai verifikasi otomatis: mengarahkan pandangan tanpa perlu mouse.
    public void ArahkanPandangan(float derajatYaw, float derajatPitch)
    {
        yaw = derajatYaw;
        pitch = Mathf.Clamp(derajatPitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (kameraPivot != null)
            kameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // Catatan: script ini sengaja TIDAK punya OnGUI. Panel info di layar Game
    // dihilangkan supaya pandangan saat bermain tidak terganggu. Yang tampil hanya
    // crosshair (dari PlayerTembak) dan bar darah musuh (dari MusuhAI).
}
