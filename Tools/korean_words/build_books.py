"""고른 단어(picks_tier1/2.txt) 검사 → 게임 단어장 CSV (Korean Word RPG).
검사: 목록에 있고 등급 맞음 · 이야기 줄에 그대로 나옴 · 단어는 두 단어장에 한 번만 · 같은 영어 뜻(쉼표로 나눈 조각) 두 번 금지 · id 겹침.
--write 를 주면 Assets/Data/Words/tier1_common.csv · tier2_advanced.csv 를 새로 씀 (예전 영단어 줄은 이야기 번호로 찾아 한국어 줄·영어 줄을 가져옴)."""
import csv, io, re, sys
from collections import Counter
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
import os
HERE = os.path.dirname(os.path.abspath(__file__)) + "/"
sys.path.insert(0, HERE)
from match import load_vocab, starts, GAME  # noqa

problems = []
POS = {"명": "n", "대": "n", "수": "n", "의": "n", "동": "v", "보": "v", "형": "adj", "관": "adj", "부": "adv", "감": "n"}
vocab = load_vocab()
bykey = {}
for e in vocab:
    bykey.setdefault(e["key"], []).append(e)


def resolve(key, mine=""):
    if key == "-":
        return None
    k, _, pos = key.partition(":")
    hits = [e for e in bykey.get(k, []) if not pos or e["pos"] == pos]
    if len(hits) > 1:   # 같은 단어가 품사만 달리 두 번 (동·보 등) → 적은 품사에 맞는 것, 보조 용언은 뒤로
        same = [e for e in hits if POS[e["pos"]] == mine and e["pos"] != "보"] or [e for e in hits if e["pos"] != "보"]
        if len({e["grade"] for e in same}) == 1:
            hits = same[:1]
    if len(hits) != 1:
        base = re.sub(r"[0-9]+$", "", k)
        alts = [f"{e['key']}:{e['pos']}{e['grade']}({e['gloss'][:6]})" for e in vocab if e["word"] == base]
        problems.append(f"목록 키 {key}: {len(hits)}개 — 후보 {' '.join(alts) or '없음'}")
        return None
    return hits[0]


def read_picks(name):
    out = []
    for line in open(HERE + name, encoding="utf-8"):
        line = line.rstrip("\n")
        if not line or line.startswith("#"):
            continue
        p = line.split("|")
        out.append(dict(story=int(p[0]), word=p[1], key=p[2], pos=p[3], meaning=p[4], roman=p[5],
                        ko=p[6] if len(p) > 6 else "", en=p[7] if len(p) > 7 else ""))
    return out


def old_story(book):
    """이야기 번호 → 한국어 줄·영어 줄·묶음 제목. 처음엔 영단어RPG CSV(예문 = 영어, 해석 = 한국어), 지금은 이 게임 CSV(예문 = 한국어)"""
    rows = list(csv.DictReader(open(GAME + book + ".csv", encoding="utf-8")))
    out = {}
    for r in rows:
        if r["story"] in ("", "0"):
            continue
        korean_first = re.search("[가-힣]", r["example"]) is not None
        ko, en = (r["example"], r["example_meaning"]) if korean_first else (r["example_meaning"], r["example"])
        out[int(r["story"])] = dict(example_meaning=ko, example=en, story_heading=r["story_heading"])
    return out


books = {"tier1_common": ("picks_tier1.txt", "A"), "tier2_advanced": ("picks_tier2.txt", "B")}
seen_words, ids = {}, Counter()
result = {}
for book, (pf, want) in books.items():
    picks = read_picks(pf)
    old = old_story(book)
    grades, parts = Counter(), {}
    for p in picks:
        e = resolve(p["key"], p["pos"])
        p["grade"] = e["grade"] if e else "-"
        p["gloss"] = e["gloss"] if e else ""
        grades[p["grade"]] += 1
        if e and e["word"] != p["word"]:
            problems.append(f"{book} {p['story']}: 목록 단어 {e['word']} ≠ {p['word']}")
        if e and POS[e["pos"]] != p["pos"] and not (POS[e["pos"]] in ("v", "adj") and p["pos"] in ("v", "adj")) and not (p["pos"] in ("adv", "adj") and e["pos"] == "명"):
            problems.append(f"{book} {p['story']} {p['word']}: 품사 목록 {e['pos']} / 적은 {p['pos']}")
        if p["story"]:
            r = old.get(p["story"])
            if r is None:
                problems.append(f"{book}: 이야기 {p['story']}번 줄이 없음"); continue
            p["ko"], p["en"], p["heading"] = r["example_meaning"], r["example"], r["story_heading"]
        else:
            p["heading"] = ""
        w = p["word"]
        verb = p["pos"] in ("v", "adj") and w.endswith("다") and len(w) > 1
        st = w[:-1] if verb else w
        toks = [re.sub(r"[^\uac00-\ud7a3]", "", t) for t in p["ko"].split()]
        if not any(starts(t, st, verb) for t in toks if t):
            problems.append(f"{book} {p['story']} {w}: 예문에 안 나옴 — {p['ko']}")
        if w in seen_words:
            problems.append(f"{w}: {seen_words[w]}과 {book} 둘 다")
        seen_words[w] = book
        for part in [x.strip().lower() for x in p["meaning"].split(",")]:
            if part in parts:
                problems.append(f"{book}: 뜻 '{part}' 두 번 — {parts[part]} · {w}")
            parts[part] = w
        p["id"] = "ko_" + re.sub(r"[^a-z]", "", p["roman"].lower())
        ids[p["id"]] += 1
    stories = sorted(p["story"] for p in picks if p["story"])
    if stories != list(range(1, len(old) + 1)):
        problems.append(f"{book}: 이야기 번호가 1~{len(old)}을 빠짐없이 채우지 않음 ({len(stories)}개)")
    print(book, len(picks), "등급", dict(grades))
    result[book] = picks

# id 겹침 → 뒤에 _2, _3 (두 단어장을 합쳐서)
count = Counter()
for book in books:
    for p in result[book]:
        if ids[p["id"]] > 1:
            count[p["id"]] += 1
            if count[p["id"]] > 1:
                p["id"] += f"_{count[p['id']]}"
print("겹친 로마자 id:", [k for k, v in ids.items() if v > 1])

# 동음이의어 확인용: 풀이가 있는 단어만
for book in books:
    print(book, "풀이:", " ".join(f"{p['word']}({p['gloss'][:6]})" for p in result[book] if p["gloss"]))

if problems:
    print("문제", len(problems))
    for x in problems:
        print(" -", x)

if "--write" in sys.argv and not problems:
    for book in books:
        with open(GAME + book + ".csv", "w", encoding="utf-8", newline="") as f:
            w = csv.writer(f, lineterminator="\n")
            w.writerow(["id", "english", "meaning", "pos", "example", "example_meaning", "story", "story_heading", "romanization"])
            for p in sorted(result[book], key=lambda p: (p["story"] == 0, p["story"])):
                w.writerow([p["id"], p["word"], p["meaning"], p["pos"], p["ko"], p["en"], p["story"] or "", p["heading"], p["roman"]])
        print("wrote", book)
