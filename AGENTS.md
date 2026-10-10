# Teknologi Game - catatan untuk agen AI

Project ini berisi dua hal: **project Unity** (game arena + AI NavMesh + tembak raycast)
dan **dokumen pendamping** (README, catatan teori, checklist bukti).

## Konteks project

- Project Unity: `UTS_TeknologiGame/` (Unity 2022.3.62f3)
- Scene utama: `Assets/Scenes/Arena.unity`
- Script gameplay: `Assets/Scripts/` (tanpa prefix, mis. `MusuhAI.cs`, `PlayerTembak.cs`)
- Script editor: `Assets/Scripts/Editor/`
- Dokumen: `README.md`, `CATATAN_TEORI.md`, `CHECKLIST_BUKTI.md`

<!-- antislop:start -->
## antislop

Untuk pekerjaan yang menyentuh tampilan layar (HUD), teks (dokumen), atau komentar kode,
baca `antislop.md` (core) sebagai filter.

Status pemasangan: **hanya core yang terpasang**. Folder `skills/` (antislop-ui,
antislop-copywriting, antislop-human, antislop-layoutmobile, antislop-code) tidak ada di
samping file core, jadi skill opsional tidak dipakai di sesi ini. Jangan mengunduhnya
saat berjalan.

Sebelum mulai, tanyakan kapan antislop dipakai: selama pengerjaan, atau setelah selesai.
<!-- antislop:end -->

## Catatan penerapan antislop di project ini

Project ini adalah **game Unity 3D**, bukan website. Sebagian besar aturan antislop
(breakpoint mobile, navbar, FAQ, CTA, pricing, footer, glassmorphism, border radius)
tidak punya padanan di sini. Yang benar-benar berlaku:

| Aturan | Bagaimana diterapkan di sini |
|---|---|
| R-02 (tanpa em dash) | berlaku untuk semua dokumen dan teks di layar |
| R-16 (tanpa buzzword) | berlaku untuk dokumen |
| R-17 / R-36 / R-38 (angka & klaim) | semua angka harus dari log verifikasi nyata, tidak boleh dikarang |
| R-25 (kontras WCAG AA) | berlaku untuk teks di layar: label bar darah musuh |
| R-26 (elemen punya fungsi) | setiap tombol di HUD harus benar-benar bekerja |
| R-31 (setiap keputusan ada alasan) | alasan ditulis di komentar kode dan di README |
| R-35 (verifikasi sebelum kirim) | jalankan `PlayModeCheck` dan ambil screenshot sebelum menyatakan selesai |

### Arah desain (dipakai sebagai pengganti DESIGN.md)

Project ini belum punya `DESIGN.md`. Arah desainnya dicatat di sini.

- **Jenis**: HUD game arena 3D, untuk dinilai dosen/penguji
- **Bahasa visual**: fungsional-bersih. HUD sengaja minimal karena pengguna meminta
  layar Game bebas dari panel yang mengganggu pandangan
- **Dial**: ENERGY 1 / RHYTHM 1 / MOTION 1
  - ENERGY 1: HUD tidak "menyapa"; hanya crosshair dan bar darah
  - RHYTHM 1: seragam dan dapat diprediksi, bukan pilihan artistik yang bervariasi
  - MOTION 1: elemen HUD diam. Gerakan di layar datang dari gameplay (proyektil,
    musuh berjalan), bukan dari animasi antarmuka
- **Aksen**: satu warna kuning-amber, dipakai di proyektil dan jejaknya. Ini motif
  identitas project: satu warna yang muncul konsisten di semua elemen "aksi menembak"
- **Kontras**: teks HUD yang berada di atas latar tidak tetap (mis. di atas lantai)
  harus punya plat gelap di belakangnya supaya tetap terbaca

### Aturan khusus project ini

1. **Jangan tambahkan panel ke layar Game tanpa diminta.** Pengguna secara eksplisit
   meminta layar Game bersih. Panel daftar periksa hanya muncul kalau
   `tampilkanPanel` dicentang di Inspector.
2. **Jangan beri collider ke objek tampilan** (proyektil, efek kena). Collider pada
   objek bergerak bisa tertangkap raycast tembakan berikutnya dan merusak hasil tembakan.
3. **Desimal selalu titik** lewat `FormatAngka`, jangan `ToString("F1")` langsung, supaya
   formatnya tidak ikut bahasa Windows.
4. **Angka di dokumen harus bisa dilacak** ke `_cek/build.log` atau `_cek/verify.log`.
   Kalau verifikasi dijalankan ulang dan angkanya berubah, perbarui dokumennya.
