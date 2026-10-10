using UnityEngine;

/// Penanda isi scene supaya ringkasannya terbaca di Inspector saat objek dipilih.
public class InfoPenanda : MonoBehaviour
{
    [TextArea(6, 20)]
    public string catatan =
        "ARENA + AI MUSUH (NAVMESH) + TEMBAK (RAYCAST)\n" +
        "\n" +
        "1. Arena & AI Musuh (NavMesh)\n" +
        "   a. Lantai arena 40x40 m + 4 dinding pembatas + 4 rintangan berkelok + 3 pilar.\n" +
        "   b. NavMesh sudah di-BAKE (menu Game > Bake NavMesh / otomatis saat scene dibuat).\n" +
        "      Hasil bake tersimpan di dalam file scene, jadi tidak perlu bake ulang.\n" +
        "   c. 4 Musuh (Capsule) memakai NavMeshAgent, otomatis mengejar Player.\n" +
        "      Karena jalurnya dihitung NavMesh, musuh memutar melewati rintangan,\n" +
        "      bukan menabrak lurus seperti kalau hanya digerakkan manual.\n" +
        "\n" +
        "2. Sistem Menembak Player (Raycasting)\n" +
        "   a. Klik kiri mouse menembak; sinar keluar dari tengah layar (crosshair)\n" +
        "      mengikuti arah pandang kamera.\n" +
        "   b. hit.distance dihitung lalu DICETAK ke Console:\n" +
        "      \"Kena Musuh pada jarak 7.5 meter\"\n" +
        "   c. Kalau raycast mengenai musuh, HP musuh berkurang 25.\n" +
        "      HP <= 0 -> musuh HANCUR (Destroy).\n" +
        "\n" +
        "Kontrol: WASD jalan | Shift lari | Space lompat | Mouse lihat |\n" +
        "         Klik kiri tembak | T tembak | B cek NavMesh | H sembunyikan HUD\n" +
        "\n" +
        "Titik penting untuk dinilai:\n" +
        "   - Window > AI > Navigation: tab Bake & tab Object terisi.\n" +
        "   - Window > General > Console: baris \"Kena Musuh pada jarak ... meter\".\n" +
        "   - Inspector Musuh_1: komponen NavMeshAgent (Speed, Stopping Distance).\n" +
        "   - Scene view: area NavMesh berwarna biru menutupi lantai dan menyisakan\n" +
        "     celah di sekitar rintangan.";
}
