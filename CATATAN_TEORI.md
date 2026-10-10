# Catatan Teori - Teknologi Game

Ringkasan untuk laporan. Setiap poin di bawah sudah dipakai di script, jadi bisa
langsung dicocokkan dengan isi kode.

---

## 1. NavMesh & NavMeshAgent

### Apa itu NavMesh
**NavMesh** (Navigation Mesh) = permukaan maya yang menandai **daerah mana yang boleh
diinjak** oleh karakter AI. Bentuknya kumpulan segitiga (poligon) yang menutupi lantai,
tetapi **menyisakan celah** di sekitar tembok dan rintangan.

Bedanya dengan collider: collider mencegah objek **menembus** benda lain, sedangkan
NavMesh memberi tahu AI **jalan mana yang bisa dilewati**. Dua hal berbeda.

### Kenapa perlu di-BAKE (soal 1b)
Bake = proses menghitung NavMesh dari bentuk geometri arena. Masukannya: objek mana saja
yang dianggap penghalang, ditambah setelan ukuran agent (radius, tinggi, kemiringan).
Keluarannya: kumpulan segitiga walkable.

Bake **tidak otomatis**. Kalau arena diubah (rintangan ditambah/dipindah) tetapi tidak
di-bake ulang, NavMesh lama tetap dipakai - musuh akan menabrak rintangan baru karena
NavMesh belum tahu ada rintangan di situ.

Syarat supaya objek ikut terhitung saat bake: objek harus ditandai
**Navigation Static** (`StaticEditorFlags.NavigationStatic`). Di scene ini, lantai dan
seluruh rintangan ditandai; **Player dan Musuh tidak** - kalau ikut ditandai, posisi awal
mereka akan dilubangi dari NavMesh dan musuh justru tidak bisa berjalan.

### Setelan agent saat bake
| Parameter | Nilai | Arti |
|---|---|---|
| `agentRadius` | 0,5 m | jarak aman dari dinding; NavMesh "digerus" 0,5 m dari tiap penghalang |
| `agentHeight` | 2 m | tinggi karakter; area dengan langit-langit lebih rendah dianggap tidak bisa dilewati |
| `agentSlope` | 45° | kemiringan maksimum yang bisa didaki |
| `agentClimb` | 0,4 m | tinggi anak tangga/undakan yang masih bisa dinaiki |
| `cellSize` | 0,1667 m | kerapatan grid perhitungan; makin kecil makin halus tapi makin berat |

Ukuran Capsule bawaan Unity = radius 0,5 / tinggi 2, jadi **pas** dengan agent type
"Humanoid" (agentTypeID 0). Kalau ukuran karakter diubah, setelan bake harus ikut diubah.

### Cara NavMeshAgent bekerja (soal 1c)
`NavMeshAgent` adalah komponen yang menggerakkan objek **di atas NavMesh**. Script kita
hanya memberi **tujuan**:

    agent.SetDestination(player.position);

Sisanya dikerjakan NavMeshAgent:
1. Mencari jalur terpendek dari posisi sekarang ke tujuan (algoritma A* di atas NavMesh).
2. Memecah jalur menjadi titik-titik belokan (`agent.path.corners`).
3. Menggerakkan objek mengikuti titik-titik itu, memperlambat di belokan, dan berhenti
   pada `stoppingDistance`.
4. Menghitung ulang jalur kalau tujuan berpindah (`autoRepath`) - ini sebabnya musuh
   terus mengikuti Player yang bergerak.

**Bukti NavMesh dipakai (bukan gerak lurus):**
- `agent.path.corners.Length > 2` -> jalur punya belokan. Kalau gerak lurus, titiknya
  hanya 2 (titik awal dan titik tujuan). Pada verifikasi, Musuh_1 jalurnya **5 titik**.
- Garis lurus musuh ke Player **terhalang rintangan** (dibuktikan dengan raycast),
  tetapi musuh tetap bisa sampai - artinya dia memutar.
- `agent.isOnNavMesh` harus `true`. Kalau posisi awal musuh di luar NavMesh, agent tidak
  jalan sama sekali dan Unity menulis peringatan "Failed to create agent because it is
  not close enough to the NavMesh".

### Hal teknis yang mudah salah
- **`baseOffset`**: NavMeshAgent menempatkan **titik asal** objek tepat di permukaan
  NavMesh. Titik asal Capsule ada di **tengah badan**, jadi tanpa `baseOffset = 1` badan
  musuh akan terbenam separuh ke dalam lantai.
- **NavMeshAgent vs transform**: jangan mengubah `transform.position` objek yang punya
  NavMeshAgent aktif - agent akan menimpanya kembali. Untuk memindahkan mendadak pakai
  `agent.Warp(posisi)`.
- **NavMeshObstacle**: dipakai untuk rintangan yang **bergerak** (pintu, peti). Bentuk
  statis cukup ditandai Navigation Static lalu di-bake.

---

## 2. Raycast (soal 2)

### Apa itu Raycast
Raycast = sinar lurus dengan **titik awal + arah + panjang maksimum**. Sinar berhenti pada
collider **pertama** yang kena - **tidak menembus** (beda dengan trigger yang bisa
ditembus).

    Ray ray = kamera.ScreenPointToRay(titikTengahLayar);   // sinar dari crosshair
    RaycastHit hit;
    bool kena = Physics.Raycast(ray, out hit, jarakMaks);

`ScreenPointToRay` mengubah titik di layar (mis. tengah layar) menjadi sinar di dunia 3D
yang keluar dari kamera. Jadi "menembak ke arah crosshair" = raycast dari titik tengah layar.

### Isi struct RaycastHit
| Anggota | Arti | Dipakai untuk |
|---|---|---|
| `hit.collider` | collider yang kena | tahu objek apa yang tertembak |
| `hit.point` | titik persis di permukaan | efek percikan, lubang peluru |
| **`hit.distance`** | **jarak dari titik awal sinar ke titik kena (meter)** | **dicetak ke Console (soal 2b)** |
| `hit.normal` | arah tegak lurus permukaan | memantulkan peluru, decal |
| `hit.rigidbody` | Rigidbody objek (kalau ada) | memberi gaya dorong |

### LayerMask - kenapa perlu
Kamera orang ketiga berada di **belakang** badan Player. Sinar dari kamera akan mengenai
badan Player lebih dulu sebelum sampai ke musuh. Karena itu Player ditaruh di layer
**"Player"** dan layer itu **dikecualikan** dari raycast:

    maskTembakan = ~(1 << LayerMask.NameToLayer("Player"));

Operator `~(1 << n)` = "semua layer KECUALI layer n". Ini jauh lebih rapi daripada
memeriksa satu per satu "apakah yang kena Player?".

### Kenapa raycast lebih baik daripada trigger untuk menembak
| | Raycast | Trigger |
|---|---|---|
| Cara kerja | sinar matematis, berhenti di objek pertama | volume yang bisa ditembus |
| Akurasi | tepat pada titik bidik | hanya "menyentuh" |
| Biaya | sangat murah (tidak perlu collider fisik bergerak) | perlu collider & Rigidbody |
| Cocok untuk | **menembak**, memilih objek, garis pandang AI | checkpoint, area damage, ambil item |

### Hal teknis yang mudah salah
- **`Physics.autoSyncTransforms`** (default `false` sejak Unity 2020): kalau objek
  dipindah lewat `transform.position`, collider-nya **belum** ikut pindah di physics
  scene sampai sinkronisasi berikutnya. Akibatnya raycast pada frame yang sama masih
  mengenai posisi LAMA. Solusinya panggil `Physics.SyncTransforms()` setelah memindahkan.
  (Ini yang sempat bikin satu pemeriksaan gagal lalu diperbaiki.)
- **Sinar tidak terlihat di Game view.** `Debug.DrawRay` / `Debug.DrawLine` hanya muncul di
  **Scene view**.
- **Dari mana jarak diukur.** `hit.distance` mengukur dari **titik asal sinar**. Karena sinar
  dibuat dari kamera, `hit.distance` = jarak **kamera** ke titik kena, bukan jarak Player ke
  musuh. Pada kamera orang ketiga, kamera berada ~6,6 m di belakang badan Player, jadi
  angkanya bisa terlihat jauh lebih besar. Kalau yang dilaporkan adalah "jarak tembakan
  Player ke musuh", hitung terpisah dengan
  `Vector3.Distance(badanPlayer.position, hit.point)`.
- **Trigger tidak terdeteksi raycast** kalau `QueryTriggerInteraction.Ignore` dipakai.
  Di sini memang di-ignore supaya zona trigger tidak menahan tembakan.

### Proyektil: kenapa dipisah dari raycast

Raycast bersifat **instan** - hasilnya sudah diketahui di frame yang sama, tapi justru
karena itu **tidak terlihat** oleh mata. Pemain hanya melihat efeknya (musuh kena),
tanpa melihat "sesuatu" yang bergerak.

Solusinya menambahkan **proyektil** sebagai lapisan tampilan:

| | Raycast | Proyektil |
|---|---|---|
| Perannya | **menentukan hasil** | **tampilan** |
| Waktu | seketika | butuh waktu terbang |
| Data | `hit.point`, `hit.distance`, `hit.collider` | posisi, kecepatan, jejak |
| Collider | tidak perlu | **sengaja tidak ada** |

Yang penting: **hasil tembakan tetap 100% dari raycast**. Proyektil hanya "dikabari"
ke mana harus terbang (`Mulai(titikKena, ...)`) dan tidak pernah ikut menentukan apa pun.
Jadi tidak ada risiko "peluru meleset tapi musuh kena" atau sebaliknya.

Kenapa proyektil **tidak boleh** diberi Collider/Rigidbody:

1. Kalau ada collider, proyektil bisa **tertangkap raycast tembakan berikutnya** dan
   membuat hasil tembakan salah (yang kena justru peluru sendiri).
2. Kalau ada Rigidbody, dia bisa **menabrak musuh secara fisik** dan mendorongnya.

Cara membuat objek visual tanpa collider: mesh diambil dari primitive sementara, lalu
primitive-nya dibuang -

    GameObject sementara = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    mesh = sementara.GetComponent<MeshFilter>().sharedMesh;
    DestroyImmediate(sementara);
    // lalu objek baru dibuat dari mesh itu + MeshRenderer (tanpa collider)

Materialnya diberi **emission** (`_EmissionColor`) supaya proyektil terlihat menyala
walau berada di area gelap. Jejak (trail) memakai `TrailRenderer`, dan efek kena memakai
bola yang membesar-mengecil lalu hilang.

Gerak proyektil dihitung manual dengan `Vector3.MoveTowards(posisi, tujuan, kecepatan * Time.deltaTime)`.
Perkalian `Time.deltaTime` wajib supaya kecepatannya sama di fps berapa pun (materi
frame-rate independent) - kalau lupa, proyektil akan bergerak lebih cepat di fps tinggi.

---

## 3. HP, damage, dan Destroy (soal 2c)

Pola yang dipakai: **pemanggil tidak mengubah HP langsung**. `PlayerTembak` hanya
memanggil `musuh.TerimaDamage(damage)`, dan `MusuhAI` sendiri yang mengurus HP-nya.

Kenapa begitu? Supaya aturan HP (berapa HP, kapan hancur, efek apa saat kena) hanya ada di
**satu tempat**. Kalau nanti damage juga datang dari sumber lain (jebakan, ledakan), cukup
panggil `TerimaDamage()` yang sama - tidak perlu menyalin logika.

    hpSekarang -= damage;
    if (hpSekarang <= 0f) Destroy(gameObject);

`Destroy(gameObject)` menghapus objek **di akhir frame** (bukan langsung), supaya aman
dipanggil di tengah perulangan/logika. Kalau perlu langsung (di dalam kode Editor),
pakai `DestroyImmediate`.

Catatan: `hpSekarang <= 0` memakai `<=`, bukan `==`. Karena HP bertipe `float`, nilainya
bisa saja tidak persis 0 (mis. damage 30 dari HP 100 -> -20). Ini kebiasaan wajib untuk
semua perhitungan bertipe pecahan.

---

## 4. Ringkasan istilah

| Istilah | Arti singkat |
|---|---|
| **NavMesh** | permukaan maya yang menandai daerah yang boleh diinjak AI |
| **Bake** | menghitung NavMesh dari geometri arena |
| **NavMeshAgent** | komponen yang menggerakkan objek di atas NavMesh |
| **Navigation Static** | tanda pada objek supaya ikut dihitung saat bake |
| **agentRadius** | jarak aman AI dari dinding |
| **baseOffset** | tinggi titik asal objek dari permukaan NavMesh |
| **path.corners** | titik-titik belokan jalur yang dihitung agent |
| **Raycast** | sinar lurus yang berhenti di objek pertama |
| **RaycastHit** | hasil raycast (collider, titik, jarak, normal) |
| **hit.distance** | jarak tembakan dalam meter |
| **Proyektil** | peluru tampilan yang terbang ke titik kena (tanpa collider) |
| **TrailRenderer** | komponen jejak di belakang proyektil |
| **Emission** | sifat material yang memancarkan cahaya sendiri (proyektil menyala) |
| **LayerMask** | filter layer mana yang diperiksa raycast |
| **CharacterController** | komponen gerak Player yang tidak bisa menembus dinding |
| **Time.deltaTime** | durasi frame terakhir, supaya gerak tidak bergantung fps |
| **Destroy** | menghapus objek (dijalankan di akhir frame) |
