"""한국어 단어장 검수 페이지: review_template.html + 게임 단어장 CSV(+ 크리스마스 선물 퀴즈 단어) + 고를 때 등급 → 한국어단어장검수.html"""
import csv, io, json, re, sys
# (출력 인코딩은 build_books가 맞춤)
import os
HERE = os.path.dirname(os.path.abspath(__file__)) + "/"
sys.path.insert(0, HERE)
from match import GAME, ROOT  # noqa
from match import starts  # noqa
from build_books import read_picks, resolve, vocab as VOCAB, POS  # noqa  (검사 출력이 한 번 나옴)


def find(sentence, word, pos):
    verb = pos in ("v", "adj") and word.endswith("다") and len(word) > 1
    stem = word[:-1] if verb else word
    for i, ch in enumerate(sentence):
        if not ("\uac00" <= ch <= "\ud7a3") or (i > 0 and "\uac00" <= sentence[i - 1] <= "\ud7a3"):
            continue
        j = i
        while j < len(sentence) and "\uac00" <= sentence[j] <= "\ud7a3":
            j += 1
        if starts(sentence[i:j], stem, verb):
            return [i, len(stem)]
    return [-1, 0]


out = {}
for book, picks in (("tier1", "picks_tier1.txt"), ("tier2", "picks_tier2.txt")):
    grade = {}
    for p in read_picks(picks):
        e = resolve(p["key"], p["pos"])
        grade[p["word"]] = e["grade"] if e else "-"
    rows = list(csv.DictReader(open(GAME + ("tier1_common" if book == "tier1" else "tier2_advanced") + ".csv", encoding="utf-8")))
    out[book] = [dict(id=r["id"], w=r["english"], r=r["romanization"], m=r["meaning"], p=r["pos"], x=r["example"], t=r["example_meaning"],
                      s=int(r["story"] or 0), g=grade[r["english"]], h=find(r["example"], r["english"], r["pos"])) for r in rows]
    print(book, len(out[book]), "강조 못 찾음:", [w["w"] for w in out[book] if w["h"][0] < 0])

# 크리스마스 선물 퀴즈 단어 (#10 — Words/Events/christmas.csv, 예문 없음). 등급 = 목록에 있으면 그 등급(품사가 같은 것), 없으면 '-'.
# 같은 단어가 목록에 둘 이상이면 XMAS_KEYS로 고름 (종 = 鐘)
XMAS_KEYS = {"종": "종13", "별": "별01"}
rows = list(csv.DictReader(open(GAME + "Events/christmas.csv", encoding="utf-8")))
out["xmas"] = []
for i, r in enumerate(rows):
    key = XMAS_KEYS.get(r["english"])
    hits = [v for v in VOCAB if (v["key"] == key if key else v["word"] == r["english"] and POS[v["pos"]] == r["pos"])]
    assert len(hits) <= 1, f"{r['english']}: 목록에 {len(hits)}개 — XMAS_KEYS에 적을 것"
    e = hits[0] if hits else None
    out["xmas"].append(dict(id=r["id"], w=r["english"], r=r["romanization"], m=r["meaning"], p=r["pos"], x="", t="", s=i + 1,
                            g=e["grade"] if e else "-", h=[-1, 0]))
print("xmas", len(out["xmas"]), "등급", {g: sum(w["g"] == g for w in out["xmas"]) for g in "ABC-"})
page =open(HERE + "review_template.html", encoding="utf-8").read()
assert page.count("__WORDS__") == 1
page = page.replace("__WORDS__", json.dumps(out, ensure_ascii=False, separators=(",", ":")))
os.makedirs(ROOT + "Builds", exist_ok=True)
open(ROOT + "Builds/한국어단어장검수.html", "w", encoding="utf-8").write(page)
print("bytes", len(page.encode("utf-8")))
