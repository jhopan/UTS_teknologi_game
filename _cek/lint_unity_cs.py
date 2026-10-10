"""Pemeriksa statis sederhana untuk file C# Unity (tanpa compiler Unity).

Yang diperiksa:
  1. keseimbangan kurung { } ( ) [ ] setelah komentar & string dibuang
  2. nama class utama == nama file (syarat MonoBehaviour di Unity)
  3. setiap tipe "TugasX_..." yang dipakai di kode benar-benar dideklarasikan
  4. tiap file diakhiri newline & tidak mengandung tab

Jalankan: python lint_unity_cs.py
"""
import glob
import os
import re
import sys

PROJECT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                       "..", "UTS_TeknologiGame", "Assets", "Scripts")


def strip_code(src):
    """Buang komentar + isi literal string/char supaya kurung di dalamnya tidak dihitung."""
    out = []
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        if c == "/" and src.startswith("//", i):
            j = src.find("\n", i)
            i = n if j < 0 else j
        elif c == "/" and src.startswith("/*", i):
            j = src.find("*/", i + 2)
            i = n if j < 0 else j + 2
        elif src.startswith('@"', i):           # string verbatim
            j = i + 2
            while j < n:
                if src[j] == '"' and src[j + 1:j + 2] != '"':
                    break
                j += 1
            i = j + 1
        elif c == '"':
            i += 1
            while i < n:
                if src[i] == "\\":
                    i += 2
                    continue
                if src[i] == '"':
                    i += 1
                    break
                i += 1
        elif c == "'":
            i += 1
            while i < n:
                if src[i] == "\\":
                    i += 2
                    continue
                if src[i] == "'":
                    i += 1
                    break
                i += 1
        else:
            out.append(c)
            i += 1
    return "".join(out)


def main():
    files = sorted(glob.glob(os.path.join(PROJECT, "**", "*.cs"), recursive=True))
    if not files:
        print("TIDAK ADA file .cs di", os.path.normpath(PROJECT))
        return 1

    problems = []
    defined_types = set()

    for path in files:
        raw = open(path, encoding="utf-8", newline="").read()
        clean = strip_code(raw)
        name = os.path.basename(path)[:-3]

        # 1. kurung seimbang
        for opener, closer, label in [("{", "}", "{}"), ("(", ")", "()"), ("[", "]", "[]")]:
            delta = clean.count(opener) - clean.count(closer)
            if delta != 0:
                problems.append("%s: kurung %s tidak seimbang (selisih %+d)" % (name, label, delta))

        # 2. nama class sesuai nama file
        classes = re.findall(r"class\s+(\w+)", clean)
        if not classes:
            problems.append("%s: tidak ada deklarasi class" % name)
        if name != "SceneBuilder" and (not classes or classes[0] != name):
            problems.append("%s: class utama harus '%s' (ditemukan %s)" % (name, name, classes))

        # 3. tab & newline akhir
        if "\t" in raw:
            problems.append("%s: mengandung karakter TAB" % name)
        if not raw.endswith("\n"):
            problems.append("%s: tidak diakhiri newline" % name)

        # 4. class di dalam namespace: cek kurung class utama ada
        if "namespace" in clean:
            problems.append("%s: memakai namespace (tidak dipakai di project ini)" % name)

        defined_types.update(classes)

    # setiap tipe yang dipakai harus terdeklarasi (mis. Tugas4_CollisionLogger)
    used = set()
    for path in files:
        for m in re.findall(r"\bTugas\d_\w+", strip_code(open(path, encoding="utf-8").read())):
            used.add(m)
    missing = sorted(t for t in used if t not in defined_types)

    print("File .cs diperiksa : %d" % len(files))
    print("Class terdeklarasi : %d -> %s" % (len(defined_types), ", ".join(sorted(defined_types))))
    print("Tipe Tugas* dipakai : %d" % len(used))
    if missing:
        problems.append("tipe dipakai tapi tidak ada: %s" % ", ".join(missing))

    if problems:
        print("\nMASALAH (%d):" % len(problems))
        for p in problems:
            print("  - " + p)
        return 1

    print("\nOK: kurung seimbang, nama class == nama file, semua tipe ada.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
