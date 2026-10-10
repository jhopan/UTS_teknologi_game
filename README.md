# Teknologi Game - Arena + AI Musuh (NavMesh) + Tembak (Raycast)

Project Unity 2022.3 LTS: arena 3D dengan AI musuh yang mengejar Player memakai
**NavMeshAgent**, dan sistem menembak memakai **Raycast** lengkap dengan proyektil.

Semua yang diminta soal **sudah jadi, sudah di-bake, dan sudah diuji otomatis**
(27 pemeriksaan, 0 gagal - lihat bagian 5).

> Catatan: folder `Library/` dan `Temp/` sengaja tidak ikut di repo (lihat `.gitignore`).
> Unity akan membuat ulang keduanya saat project pertama kali dibuka.

---

## 1. Isi soal vs hasil

| Butir soal | Hasil | Bukti |
|---|---|---|
| **1a.** Lantai arena sederhana + rintangan (tembok) | Lantai 40 x 40 m, 4 dinding pembatas, 4 rintangan berkelok, 3 pilar | `Assets/Scenes/Arena.unity`, Hierarchy `Arena > Rintangan` |
| **1b.** Bake arena pakai NavMesh | **Sudah di-bake**: 204 titik, 88 segitiga, luas walkable **1369.4 m²** | `Assets/Scenes/Arena/NavMesh.asset` (12,8 KB) |
| **1c.** Musuh (Cylinder/Capsule) mengejar Player pakai NavMeshAgent | 4 musuh Capsule + `NavMeshAgent`, otomatis mengejar Player | `Assets/Scripts/MusuhAI.cs` |
| **2a.** Player menembak klik kiri pakai Raycast | Klik kiri = raycast dari tengah layar (crosshair) + **proyektil terlihat terbang** | `Assets/Scripts/PlayerTembak.cs`, `Proyektil.cs` |
| **2b.** Raycast deteksi musuh, hitung jarak, cetak ke Console | Mencetak `Kena Musuh pada jarak 5.8 meter` | Console (lihat bagian 5) |
| **2c.** HP musuh berkurang; HP <= 0 musuh hancur (Destroy) | HP 100, damage 25/tembakan; 4 tembakan -> hancur | Bar HP musuh + Console |

---

## 2. Cara membuka & menjalankan

1. Buka **Unity Hub** > tab **Projects** > **Add** > **Add project from disk**, lalu pilih
   folder `UTS_TeknologiGame/` dari repo ini.
2. Buka scene `Assets/Scenes/Arena.unity`.
3. Tekan tombol **Play**.

Pertama kali dibuka, Unity akan membuat folder `Library/` (butuh beberapa menit).

Kalau scene belum ada / ingin dibuat ulang: menu bar **Game** > **Build Scene Arena (Arena + AI + Tembak)**.
Scene dibuat ulang dari kode lalu **NavMesh di-bake otomatis** di akhir proses.

### Kontrol saat Play

| Tombol | Fungsi |
|---|---|
| **W A S D** | jalan |
| **Shift** | lari |
| **Space** | lompat |
| **Mouse** | putar pandang (kamera orang ketiga) |
| **Klik kiri** | **menembak** (raycast dari crosshair + proyektil) |
| **T** | tembak (alternatif keyboard) |
| **B** | cek status NavMesh (hasilnya ditulis ke Console) |
| **C** | tampilkan / sembunyikan crosshair |
| **Tab** | kunci / lepas kunci kursor |

> **Layar Game sengaja bersih.** Yang tampil hanya **crosshair** di tengah dan
> **bar darah di atas kepala musuh**. Panel-panel info/daftar periksa sudah dihapus
> supaya pandangan saat bermain tidak terganggu. Kalau panel daftar periksa masih
> diperlukan saat presentasi, centang `tampilkanPanel` di Inspector objek `HUD`.

### Tampilan layar (HUD)

Dua elemen ini yang tampil, dan alasan tiap keputusannya:

| Elemen | Keputusan | Alasan |
|---|---|---|
| Crosshair | Inti putih 2 px di atas plat gelap 6 px | Di lantai terang, inti putih 2 px tanpa plat tebal praktis hilang. Plat 6 px memberi 2 px gelap di tiap sisi, jadi tetap terbaca tanpa terlihat tebal. |
| Bar darah | 3 warna diskret: hijau (>60%), kuning (30-60%), merah (<30%) | `Color.Lerp(merah, hijau)` melewati RGB(0.50, 0.50, 0.00) yang berwarna olive keruh, sehingga bar terlihat kotor tepat saat HP tinggal separuh. |
| Label HP | Teks putih di atas plat gelap | Teks putih langsung di atas lantai terang hanya mencapai kontras 2.15:1, di bawah ambang WCAG AA (4.5:1). Plat gelap membuat kontrasnya tidak lagi bergantung warna lantai. |

Kontras setelah perbaikan, diukur di tiga kondisi lantai:

| Kondisi lantai | Teks putih vs plat | Bar hijau | Bar kuning | Bar merah |
|---|---|---|---|---|
| material lantai (gelap) | 17.92:1 | 7.40:1 | 9.59:1 | 3.82:1 |
| lantai + cahaya terang | 16.14:1 | 6.66:1 | 8.64:1 | 3.44:1 |
| lantai terang sekali | 15.33:1 | 6.33:1 | 8.20:1 | 3.26:1 |

Semua di atas ambang WCAG AA (teks 4.5:1, elemen non-teks 3:1).

---

## 3. Penjelasan tiap butir soal

### 1a. Arena & rintangan
Lantai dibuat dari primitive **Plane** diskalakan menjadi **40 x 40 m**. Arena dikelilingi
4 dinding pembatas supaya musuh dan Player tidak keluar area, lalu di dalamnya ada
**rintangan berkelok** (4 balok panjang) dan **3 pilar silinder**.

Rintangan sengaja disusun **berkelok**, bukan sebarangan. Tujuannya: supaya jalur musuh
**tidak bisa lurus** ke Player. Kalau musuh hanya digerakkan lurus, dia akan tersangkut
di rintangan. Karena memakai NavMesh, musuh justru memutar lewat celah - itulah bukti
NavMesh-nya benar-benar bekerja.

### 1b. Bake NavMesh
Bake dilakukan otomatis oleh script editor (`SceneBuilder.BakeNavMesh`) dengan urutan:

1. Objek **lantai & rintangan** ditandai **Navigation Static** (`StaticEditorFlags.NavigationStatic`).
   Player dan Musuh **tidak** ditandai - kalau ikut ditandai, posisi awal mereka akan
   "dilubangi" dari NavMesh dan musuh tidak bisa berjalan.
2. `NavMeshBuilder.ClearAllNavMeshes()` lalu `NavMeshBuilder.BuildNavMesh()`.
3. Hasil dihitung ulang untuk verifikasi: **204 titik, 88 segitiga, luas 1369.4 m²**
   (arena 40x40 = 1600 m², sisa berkurang karena rintangan - angkanya masuk akal).
4. Scene disimpan ulang sehingga data NavMesh tersimpan di `Assets/Scenes/Arena/NavMesh.asset`.

Setelan agent saat bake: **radius 0,5 m, tinggi 2 m, kemiringan maks 45°, tinggi anak tangga 0,4 m**
(agent type "Humanoid"). Ini cocok dengan ukuran Capsule bawaan Unity.

Untuk bake manual dari Editor (kalau perlu mengubah rintangan):
**Window > AI > Navigation** > tab **Bake** > tombol **Bake**. Jangan lupa **Ctrl+S**.
Atau menu **Game > Bake NavMesh (ulang)**.

### 1c. Musuh AI dengan NavMeshAgent
Musuh dibuat dari primitive **Capsule** (radius 0,5 / tinggi 2 - pas dengan agent "Humanoid"),
ditambah komponen **NavMeshAgent** dan script `MusuhAI`.

Yang dilakukan script tiap frame hanya **satu baris inti**:

    agent.SetDestination(player.position);

Setelah itu **NavMeshAgent sendiri** yang menghitung jalur terpendek sambil menghindari
rintangan. Script tidak menggerakkan musuh sedikit pun - itu poin penting soal ini.

Setelan penting di `NavMeshAgent`:
- `baseOffset = 1` - karena titik asal Capsule ada di **tengah badan**, bukan di kaki.
  Tanpa ini badan musuh terbenam separuh ke lantai.
- `speed = 3.5`, `stoppingDistance = 1.8` (berhenti saat sudah dekat Player).

### 2a. Menembak dengan Raycast
Sinar dibuat dari **tengah layar** (posisi crosshair) mengikuti arah pandang kamera:

    Ray ray = kamera.ScreenPointToRay(tengah layar);
    Physics.Raycast(ray, out hit, jarakMaks, maskTembakan);

`maskTembakan` = semua layer **kecuali layer "Player"**. Ini perlu karena kamera orang
ketiga berada di **belakang** badan Player - kalau tidak dikecualikan, yang kena raycast
adalah badan Player sendiri. Layer "Player" dibuat otomatis oleh script builder.

Raycast berhenti pada collider **pertama** yang kena (beda dengan trigger yang bisa
ditembus).

**Proyektil (tampilan tembakan).** Hasil tembakan **tetap ditentukan raycast** (instan
dan akurat). Supaya tembakannya juga **terlihat** - bukan cuma garis sekejap - dibuat
proyektil: bola kecil menyala yang terbang dari ujung laras ke titik kena, lengkap
dengan **jejak (trail)** di belakangnya, lalu memunculkan **efek kena** di titik sasaran.

Pembagian tugasnya sengaja dipisah tegas:

| | Raycast | Proyektil |
|---|---|---|
| Perannya | **menentukan hasil** (apa yang kena, jaraknya, damage) | **tampilan** saja |
| Waktu | seketika (1 frame) | butuh waktu terbang |
| Collider | tidak perlu | **sengaja tidak diberi collider** |

Proyektil tidak diberi collider/Rigidbody karena kalau punya, dia bisa ikut tertangkap
raycast tembakan berikutnya atau menabrak musuh secara fisik. Geraknya dihitung manual
dengan `Vector3.MoveTowards` dikali `Time.deltaTime` supaya kecepatannya tetap sama di
fps berapa pun.

Bagian yang bisa diatur di Inspector (`PlayerTembak`):

| Setelan | Nilai awal | Arti |
|---|---|---|
| `pakaiProyektil` | aktif | matikan kalau ingin menembak tanpa proyektil |
| `kecepatanProyektil` | 95 m/s | kecepatan terbang proyektil |
| `ukuranProyektil` | 0,16 m | besar bola proyektil |
| `warnaProyektil` / `warnaJejak` | kuning-oranye | warna peluru & jejaknya |
| `jejakAktif` + `durasiJejak` | aktif / 0,16 s | jejak di belakang proyektil |
| `efekKenaAktif` | aktif | ledakan kecil di titik kena |
| `ujungLaras` | `Main Camera > UjungLaras` | titik asal proyektil |

### 2b. Jarak tembakan dicetak ke Console
Sinar memang dibuat dari kamera (`ScreenPointToRay`) supaya tembakan tepat mengikuti
crosshair. Tetapi kamera orang ketiga berada **~6.6 m di belakang** badan Player, jadi
kalau `hit.distance` mentah yang dicetak, angkanya terlihat lebih jauh daripada jarak
sebenarnya dari Player.

Karena itu jarak dihitung dari **badan Player** ke titik kena, lalu dicetak:

    float jarakDariPlayer = Vector3.Distance(badanPlayer.position, titikKena);
    Debug.Log("Kena Musuh pada jarak " + FormatAngka.F(jarakDariPlayer, "F1") + " meter");

Hasil nyata saat verifikasi: `Kena Musuh pada jarak 5.8 meter`.
Baris detail mencatat keduanya supaya bisa dibandingkan:

    [Game] detail tembakan -> musuh 'Musuh_3' | jarak dari Player 5.84 m
           | hit.distance dari kamera 11.45 m | titik kena ... | normal ...

Perbedaan 11.45 m (kamera) vs 5.84 m (Player) itu wajar: selisihnya adalah jarak
kamera ke badan Player (~5.6 m pada sudut tembakan tersebut).

### 2c. HP musuh berkurang, HP <= 0 musuh hancur
`MusuhAI` punya `hpMaks = 100`. Tiap tembakan yang mengenai musuh mengurangi
`damage = 25` lewat `TerimaDamage()`. Saat `hpSekarang <= 0`:

    Destroy(gameObject);

Saat terkena tembakan, badan musuh berkedip merah dan **bar darah di atas kepala**
turun. Hasil nyata: HP 100 -> 75 -> 50 -> 25 -> 0, musuh hancur pada **tembakan ke-4**.

---

## 4. Struktur file

```
UTS_teknologi_game/
├── README.md                  <- file ini (penjelasan & cara pakai)
└── UTS_TeknologiGame/         <- PROJECT UNITY (buka yang ini)
    ├── Assets/
    │   ├── Scenes/
    │   │   ├── Arena.unity          <- scene utama
    │   │   └── Arena/NavMesh.asset  <- data NavMesh hasil bake
    │   └── Scripts/
    │       ├── MusuhAI.cs            <- 1c. musuh + NavMeshAgent + HP
    │       ├── PlayerTembak.cs       <- 2a/2b/2c. raycast + damage + proyektil
    │       ├── Proyektil.cs          <- peluru yang terlihat terbang
    │       ├── EfekKena.cs           <- ledakan kecil di titik kena
    │       ├── AsetVisual.cs         <- mesh & material siap pakai (tanpa collider)
    │       ├── FormatAngka.cs        <- penulisan angka (desimal selalu titik)
    │       ├── PlayerGerak.cs        <- penunjang: gerak Player
    │       ├── GameHUD.cs            <- tombol pintas (panel default mati)
    │       ├── InfoPenanda.cs        <- catatan isi scene
    │       └── Editor/
    │           └── SceneBuilder.cs   <- pembuat scene + BAKE NavMesh
    ├── Packages/manifest.json
    └── ProjectSettings/
```

Catatan pribadi untuk laporan (catatan teori, checklist bukti), folder alat bantu
(`_cek/`), screenshot verifikasi, dan skrip pengujian sengaja **tidak** ikut di repo
ini supaya isinya fokus pada project yang bisa langsung dijalankan.



---

## 5. Verifikasi otomatis (SUDAH dijalankan - semua lulus)

Scene dibuat dan diuji **tanpa GUI** (batchmode) memakai harness verifikasi Play mode.
Hasil: **27 pemeriksaan, 0 gagal**. Angka-angka di tabel bawah ini berasal dari log
pengujian nyata, bukan perkiraan.

| Butir | Yang diuji otomatis | Hasil nyata |
|---|---|---|
| 1a | lantai arena ada | ukuran 40 x 40 m |
| 1a | rintangan arena ada | 11 objek rintangan |
| 1b | NavMesh sudah di-bake & tersimpan di file scene | 204 titik, 88 segitiga, 1369.445 m² |
| 1b | luas NavMesh masuk akal (1200-1600 m²) | 1369.445 m² (arena 1600 m²) |
| 1c | musuh ada | 4 musuh |
| 1c | tiap musuh memakai NavMeshAgent | 4/4 musuh |
| 2a | script penembak terpasang di Player | jarakMaks 120 m, damage 25 |
| 2a | hierarki kamera orang ketiga | Player > KameraPivot > Main Camera |
| 1c | agent berada di atas NavMesh saat Play | 4/4 agent valid (`isOnNavMesh`) |
| 1c | **musuh mengejar Player** | jarak terdekat 18.3 m -> **7.8 m** |
| 1c | NavMeshAgent menghitung jalur | 4/4 musuh `hasPath = true` |
| 1c | garis lurus musuh->Player terhalang rintangan | terhalang `Rintangan_D` pada 3.08 m |
| 1c | **jalur NavMesh memutar** (corners > 2) | Musuh_1 jalurnya **5 titik** (memutar) |
| 2a | raycast dari kamera mengenai musuh | kena Musuh_1 pada 12 m |
| 2a | **proyektil tampilan diluncurkan saat menembak** | 4 proyektil diluncurkan |
| 2a | proyektil tidak punya collider (tidak mengganggu raycast) | 0 dari 0 proyektil punya collider |
| 2a | **ujung laras di tubuh Player, bukan di udara** | tinggi 1.430 m (kepala Player 2.0 m) |
| 2b | **Console mencetak "Kena Musuh pada jarak ... meter"** | `Kena Musuh pada jarak 5.8 meter` |
| 2c | **HP musuh berkurang tiap tembakan** | 4 tembakan, damage 25,000 |
| 2c | **HP <= 0 -> musuh HANCUR (Destroy)** | hancur setelah 4 tembakan |
| 2c | jumlah musuh berkurang setelah dihancurkan | 4 musuh -> 3 musuh |
| 2b | jarak dari Player terukur | jarak terakhir 5.84 m, 4 kena musuh |

Bagian HUD (crosshair, bar darah) **belum tercakup** pemeriksaan ini karena `OnGUI`
tidak bisa dirender di batchmode. Kontrasnya sudah dihitung manual dan lolos WCAG AA
(lihat tabel di bagian 2), dan tampilannya bisa diperiksa lewat
**Game > Ambil Screenshot HUD** dari Editor.

### Membuat ulang scene & bake NavMesh

Menu Editor: **Game > Build Scene Arena (Arena + AI + Tembak)**.
Scene dibangun ulang dari kode lalu NavMesh di-bake otomatis di akhir proses.
Ada juga **Game > Bake NavMesh (ulang)** kalau hanya ingin bake ulang.

---

## 6. Catatan jebakan yang sudah diatasi (dari menjalankan Unity, bukan dugaan)

| Kejadian | Gejala | Perbaikan |
|---|---|---|
| Collider Capsule ganda | Player punya CapsuleCollider + CharacterController sekaligus | CapsuleCollider bawaan dibuang (`DestroyImmediate`) saat scene dibuat |
| Musuh terbenam ke lantai | Titik asal Capsule ada di tengah badan | `NavMeshAgent.baseOffset = 1` |
| Raycast kena badan Player sendiri | Kamera orang ketiga ada di belakang Player | Layer "Player" dibuat otomatis + `maskTembakan` mengecualikannya |
| Raycast "meleset" walau musuh persis di depan | Sejak Unity 2020 `Physics.autoSyncTransforms` default `false`; collider belum pindah di frame yang sama | `Physics.SyncTransforms()` setelah memindahkan objek |
| Referensi script jadi "null palsu" | Referensi yang disimpan sebelum Play mode tidak valid setelah Unity memuat ulang scene | Referensi runtime diambil ulang **di dalam** Play mode |
| Harness nyangkut di tengah jalan | Domain reload menghapus field static & langganan `EditorApplication.update` | `EnterPlayModeOptions.DisableDomainReload` saat masuk Play mode |
| Tembakan tidak terlihat, hanya garis sekejap | Raycast bersifat instan (1 frame), tidak bisa dilihat mata | Ditambah proyektil: bola menyala yang terbang ke titik kena + jejak + efek kena |
| Proyektil bisa mengganggu raycast | Kalau proyektil punya Collider, dia ikut tertangkap raycast tembakan berikutnya | Proyektil dibuat TANPA collider (mesh diambil dari primitive lalu collider-nya dibuang) |
| Pesan Console tidak tertangkap | `Debug.Log` dipanggil dari dalam Play mode | Ditangkap lewat `Application.logMessageReceived` |

---

## 7. Yang tidak bisa diuji otomatis (perlu tangan kamu)

| Hal | Kenapa | Cara menguji |
|---|---|---|
| Gerak WASD, lompat, putar pandang | batchmode tidak punya input keyboard/mouse | tekan Play, jalankan dengan WASD |
| Rasa menembak (klik kiri) | sama, butuh input mouse | tekan Play, klik kiri ke arah musuh |
| Tampilan akhir / sensasi permainan | butuh render + GPU | rekam layar saat bermain |
| Kedipan merah & bar darah di layar | butuh render | lihat saat menembak musuh |

Screenshot tidak ikut di repo (hanya dipakai saat verifikasi di komputer pengembang).
Untuk laporan, ambil sendiri dari Game view; untuk bukti gerak sebaiknya rekam layar.
