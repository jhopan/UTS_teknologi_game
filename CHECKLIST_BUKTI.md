# Checklist Bukti - Teknologi Game

Daftar bukti yang perlu dikumpulkan untuk laporan/presentasi.
Tanda **[otomatis]** = sudah tersedia di folder ini, tidak perlu dikerjakan ulang.

---

## A. Sudah tersedia (hasil otomatis)

- [x] **[otomatis]** Scene `Assets/Scenes/Arena.unity` - sudah dibuat & di-bake
- [x] **[otomatis]** Data NavMesh `Assets/Scenes/Arena/NavMesh.asset` (12,8 KB)
- [x] **[otomatis]** Log bake NavMesh - `_cek/build.log`
      baris: `[Game] BAKE NAVMESH SELESAI -> 204 titik, 88 segitiga, luas area walkable 1369.4 m2.`
- [x] **[otomatis]** Log verifikasi otomatis - `_cek/verify.log` (26 CEK PASS, 0 FAIL)
- [x] **[otomatis]** 5 screenshot hasil render - folder `UTS_TeknologiGame/Bukti_Screenshot/`
  - [x] `01_arena_dari_atas.png` - tampak keseluruhan arena 40x40 m + rintangan + Player + 4 musuh
  - [x] `02_musuh_mengejar_player.png` - musuh bergerak mendekati Player
  - [x] `03_rintangan_berkelok.png` - rintangan berkelok (jalur musuh memutar)
  - [x] `04_sudut_pandang_player.png` - sudut pandang dekat Player & musuh
  - [x] `05_proyektil_terbang.png` - **proyektil di tengah jalur tembakan + jejaknya**
- [ ] **[perlu dijalankan]** Screenshot HUD (crosshair + bar darah). Ambil lewat menu
      **Game > Ambil Screenshot HUD**. Screenshot biasa tidak menangkap HUD karena
      `Camera.Render()` tidak menggambar `OnGUI`.

---

## B. Screenshot yang perlu diambil sendiri (butuh layar/GPU)

Ambil sambil scene `Arena` dalam keadaan **Play**.

### B1. Soal 1a - Arena & rintangan
- [ ] **Game view**: tampak lantai arena, dinding pembatas, rintangan, pilar
- [ ] **Hierarchy** dibuka: terlihat `Arena > Lantai_Arena` dan `Arena > Rintangan` (11 anak)
- [ ] **Inspector** objek `Rintangan_A`: `Static` tercentang (buka dropdown > `Navigation Static` ✓)

### B2. Soal 1b - Bake NavMesh
- [ ] **Window > AI > Navigation** > tab **Bake**: tampak setelan agent
      (Agent Radius 0.5, Agent Height 2, Max Slope 45, Step Height 0.4)
- [ ] **Scene view** dengan tampilan NavMesh aktif (tombol "Show NavMesh" di toolbar
      atau `Window > AI > Navigation` > tab Bake > toggle visibilitas):
      **area biru** menutupi lantai dan **menyisakan celah** di sekeliling rintangan
- [ ] **Window > AI > Navigation** > tab **Object**: terlihat 12 objek bertanda
      "Navigation Static"
- [ ] `Assets/Scenes/Arena/NavMesh.asset` terlihat di **Project** window

### B3. Soal 1c - Musuh mengejar Player
- [ ] **Inspector** objek `Musuh > Musuh_1`: komponen **NavMeshAgent** terlihat
      (Speed 3.5, Stopping Distance 1.8, Radius 0.5, Height 2, Base Offset 1)
- [ ] **Scene view** saat Play: objek `Musuh_1` terpilih -> garis jalur NavMesh
      (garis putih/abu) terlihat **memutar melewati rintangan**, bukan lurus
- [ ] **Rekam layar 20-30 detik**: Player diam di pojok, keempat musuh datang dari
      arah berbeda sambil memutar melewati rintangan sampai mengerumuni Player
- [ ] **Game view**: keempat musuh terlihat bergerak mendekat sambil memutar lewat rintangan
      (panel daftar periksa sudah dihapus; kalau perlu, tekan **B** lalu lihat Console:
      `[Game] status NavMesh -> 4/4 agent di atas NavMesh, ...`)

### B4. Soal 2a & 2b - Menembak dengan Raycast
- [ ] **Game view**: crosshair terlihat di tengah layar (layar bersih tanpa panel)
- [ ] **Game view**: klik kiri -> **proyektil terlihat terbang** dari ujung laras ke musuh,
      dengan **jejak** di belakangnya dan **efek kena** di titik sasaran
- [ ] **Console** dibuka (**Window > General > Console**) - pastikan filter tidak menyembunyikan
      log biasa
- [ ] Arahkan crosshair ke musuh, **klik kiri** -> di Console muncul baris:
      `Kena Musuh pada jarak ... meter`
      **Screenshot Console-nya** (ini bukti utama soal 2b)
- [ ] **Game view**: bar darah di atas kepala musuh turun sesuai tembakan
      (jarak tembakan juga tercatat di Console; tidak ada lagi panel di layar)

### B5. Soal 2c - HP berkurang & musuh hancur
- [ ] Tembak satu musuh sekali -> **bar darah di atas kepala musuh turun** (100 -> 75)
      dan badan musuh **berkedip merah**. Screenshot momen ini.
- [ ] Console menampilkan:
      `[Game] Musuh 'Musuh_1' kena 25 damage -> HP 75 / 100`
- [ ] Lanjutkan menembak sampai HP habis -> Console menampilkan:
      `[Game] HP musuh 'Musuh_1' habis (<= 0) -> MUSUH HANCUR (Destroy).`
      dan musuh **hilang dari Hierarchy**
- [ ] **Hierarchy** sebelum & sesudah: jumlah `Musuh_N` berkurang 1 (4 -> 3)
- [ ] **Rekam layar 20-30 detik**: seluruh proses dari tembakan pertama sampai musuh hancur

### B6. Bukti tambahan (nilai plus)
- [ ] **Inspector** objek `Player`: komponen `CharacterController`, `PlayerGerak`,
      `PlayerTembak`
- [ ] **Inspector** objek `Player`: `Layer = Player` (bukti kenapa tembakan tidak
      mengenai badan sendiri)
- [ ] Tekan **B** saat Play -> Console menampilkan jumlah agent valid di atas NavMesh
      dan jumlah segitiga NavMesh
- [ ] **Hierarchy** objek `Info_Scene` dipilih -> catatan isi scene terbaca di Inspector

---

## C. Video yang sebaiknya direkam

| No | Durasi | Isi |
|---|---|---|
| 1 | 20-30 dtk | Musuh mengejar Player sambil memutar melewati rintangan |
| 2 | 20-30 dtk | Menembak musuh: **proyektil terbang** + Console + bar darah + musuh hancur |
| 3 | 10-15 dtk | Scene view saat Play: garis jalur NavMesh musuh terlihat memutar |

Saran: rekam **Game view dan Scene view berdampingan** pada video no. 3, supaya terlihat
jelas bahwa jalur musuh memang memutar dan bukan garis lurus.

Untuk menampilkan panel daftar periksa saat rekaman (kalau diperlukan), centang
`tampilkanPanel` di Inspector objek `HUD`.

---

## D. Urutan pengambilan yang disarankan

1. Buka project & scene `Arena.unity`.
2. Ambil B1 dan B2 (boleh saat belum Play).
3. Tekan **Play**.
4. Ambil B3 (biarkan musuh mendekat dulu ~10 detik).
5. Ambil B4 dan B5 (menembak) - **Console harus terlihat**.
6. Ambil B6 sebagai pelengkap.
7. Hentikan Play, rapikan screenshot ke dalam satu folder untuk laporan.
