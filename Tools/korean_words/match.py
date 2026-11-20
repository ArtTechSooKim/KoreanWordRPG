"""영단어RPG 이야기의 한국어 줄마다, 줄 안에 그대로 나오는 학습용 어휘(A·B·C)를 찾는다.
붙여 쓴 말(어절)의 앞부분이 단어(동사·형용사는 '다'를 뗀 줄기)와 자모 단위로 같으면 후보.
결과: candidates.json (줄마다 후보 목록) + 짝짓기(한 줄에 한 단어, 단어는 한 번만) 시험."""
import csv, json, re, sys, io
if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
import os
HERE = os.path.dirname(os.path.abspath(__file__)) + "/"
ROOT = os.path.normpath(HERE + "../..") + "/"
GAME = ROOT + "Assets/Data/Words/"
VOCAB = ROOT + "Docs/한국어_학습용_어휘목록_국립국어원2003.tsv"

CHO = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ"
JUNG = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ"
JONG = ["", "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ", "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ", "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"]


def jamo(s):
    out = []
    for ch in s:
        c = ord(ch) - 0xAC00
        if 0 <= c < 11172:
            out.append(CHO[c // 588]); out.append(JUNG[(c % 588) // 28])
            if c % 28: out.append(JONG[c % 28])
        else:
            out.append(ch)
    return "".join(out)


def syl(ch):
    c = ord(ch) - 0xAC00
    return (c // 588, (c % 588) // 28, c % 28) if 0 <= c < 11172 else None


def starts(token, st, verb):
    """어절이 단어로 시작하는지. 명사 등은 글자 그대로, 동사·형용사 줄기는 마지막 글자에 받침(ㄴㄹㅁㅂㅆ)이 붙어도 됨 (간다·갈·갑니다·갔다)"""
    if token.startswith(st):
        return True
    if not verb or len(token) < len(st) or not token.startswith(st[:-1]):
        return False
    a, b = syl(st[-1]), syl(token[len(st) - 1])
    if not a or not b or a[0] != b[0]:
        return False
    if a[2] == 0 and a[1] != b[1]:     # 줄기 끝 모음 + 어/아 줄임: 지+어→져 · 이+어→여 · 모으+아→모아 · 하+였→했 · 보+았→봤 · 주+었→줘
        va, vb = JUNG[a[1]], JUNG[b[1]]
        ok = {"ㅣ": "ㅕ", "ㅗ": "ㅘ", "ㅜ": "ㅝ", "ㅚ": "ㅙ", "ㅡ": "ㅓㅏ"}.get(va, "")
        if st[-1] == "하":
            ok = "ㅐ"
        return vb in ok and JONG[b[2]] in ("", "ㅆ")
    if a[1] != b[1]:
        return False
    ja, jb = JONG[a[2]], JONG[b[2]]
    if ja == "":                       # 가 → 간·갈·감·갑·갔
        return jb in ("ㄴ", "ㄹ", "ㅁ", "ㅂ", "ㅆ")
    if ja == "ㄹ":                     # 살다 → 사는·산다·삽니다 (ㄹ 탈락)
        return jb in ("", "ㄴ", "ㅂ")
    if ja == "ㄷ":                     # 듣다 → 들어·들었다 (ㄷ 불규칙)
        return jb == "ㄹ"
    return False


def load_vocab():
    rows = []
    for line in open(VOCAB, encoding="utf-8").read().splitlines():
        if line.startswith("#") or line.startswith("순위"):
            continue
        p = line.split("\t")
        if len(p) < 5 or not p[1]:
            continue
        rank, word, pos, gloss, grade = p[0], p[1], p[2], p[3], p[4].strip()
        base = re.sub(r"\d+$", "", word)
        rows.append(dict(rank=int(rank) if rank.isdigit() else 99999, key=word, word=base, pos=pos, gloss=gloss, grade=grade))
    return rows


def stem(entry):
    w = entry["word"]
    if entry["pos"] in ("동", "형", "보") and w.endswith("다") and len(w) > 1:
        return w[:-1]
    return w


def candidates(line, vocab):
    toks = [re.sub(r"[^\uac00-\ud7a3]", "", t) for t in line.split()]
    toks = [t for t in toks if t]
    tj = [jamo(t) for t in toks]
    found = []
    for e in vocab:
        if e["pos"] not in ("명", "동", "형", "부", "대", "수", "관"):
            continue
        st = stem(e)
        verb = e["pos"] in ("동", "형", "보") and st != e["word"]
        for t in toks:
            if starts(t, st, verb):
                found.append(dict(key=e["key"], word=e["word"], pos=e["pos"], grade=e["grade"], rank=e["rank"], gloss=e["gloss"], token=t))
                break
    return found


def main():
    vocab = load_vocab()
    print("vocab", len(vocab), {g: sum(1 for v in vocab if v["grade"] == g) for g in "ABC"})
    out = {}
    for book in ("tier1_common", "tier2_advanced"):
        rows = list(csv.DictReader(open(GAME + book + ".csv", encoding="utf-8")))
        lines = []
        for r in rows:
            n = int(r["story"] or 0)
            lines.append(dict(oldId=r["id"], story=n, heading=r["story_heading"], ko=r["example_meaning"], en=r["example"],
                              cands=candidates(r["example_meaning"], vocab)))
        out[book] = lines
        for g in "ABC":
            empty = sum(1 for l in lines if not any(c["grade"] == g for c in l["cands"]))
            print(book, "lines", len(lines), f"no {g} candidate:", empty)
    json.dump(out, open(HERE + "candidates.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
