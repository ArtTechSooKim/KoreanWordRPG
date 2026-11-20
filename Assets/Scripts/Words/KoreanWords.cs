namespace WordRPG.Words
{
    // 한국어 단어가 예문 어디에 나오는지 (#1 Korean Word RPG). 영어처럼 낱말이 그대로 나오지 않는다:
    // 명사 = 어절(띄어 쓴 덩어리) 앞에 그대로 + 조사 (이름이·이름을), 동사·형용사 = '다'를 뗀 줄기 + 어미 (가르치는·읽고).
    // 줄기의 마지막 글자는 어미와 합쳐져 모양이 바뀔 수 있다 — 받침이 붙음(간·갈·갑·갔), ㄹ 탈락(살다 → 산다),
    // ㄷ 불규칙(듣다 → 들었다), 모음 줄임(사라지 + 어 → 사라져, 모이 + 어 → 모여, 하 + 였 → 했). 그 밖의 불규칙(ㅂ·르·ㅎ)은 예문을 고를 때 피함
    public static class KoreanWords
    {
        private const int HangulBase = 0xAC00, HangulCount = 11172;
        // 받침 번호 (유니코드 순서): 0 없음 · 4 ㄴ · 7 ㄷ · 8 ㄹ · 16 ㅁ · 17 ㅂ · 20 ㅆ
        private const int NoFinal = 0, FinalN = 4, FinalD = 7, FinalL = 8, FinalM = 16, FinalB = 17, FinalSs = 20;
        // 모음 번호: 0 ㅏ · 1 ㅐ · 4 ㅓ · 6 ㅕ · 8 ㅗ · 9 ㅘ · 10 ㅙ · 11 ㅚ · 13 ㅜ · 14 ㅝ · 18 ㅡ · 20 ㅣ
        private const int InitialH = 18;

        public static bool IsHangul(char c) => c >= HangulBase && c < HangulBase + HangulCount;

        public static bool HasHangul(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
                if (IsHangul(c)) return true;
            return false;
        }

        // 동사·형용사처럼 '다'로 끝나 어미가 붙는 말인지 (관형사 '새'·'그'나 명사 '바다'는 아님)
        public static bool Conjugates(string word, string partOfSpeech)
        {
            return (partOfSpeech == "v" || partOfSpeech == "adj") && word.Length > 1 && word[word.Length - 1] == '다';
        }

        // 예문에서 단어가 처음 나오는 자리. 동사는 줄기 길이만큼 (사라져 → '사라져'). 없으면 start = -1
        public static (int start, int length) Find(string sentence, string word, string partOfSpeech)
        {
            if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(word)) return (-1, 0);
            bool verb = Conjugates(word, partOfSpeech);
            string stem = verb ? word.Substring(0, word.Length - 1) : word;
            for (int i = 0; i < sentence.Length; i++)
            {
                if (!IsHangul(sentence[i])) continue;
                if (i > 0 && IsHangul(sentence[i - 1])) continue; // 어절 첫 글자만 (따옴표 다음은 됨)
                if (StartsWith(sentence, i, stem, verb)) return (i, stem.Length);
            }
            return (-1, 0);
        }

        private static bool StartsWith(string text, int at, string stem, bool verb)
        {
            if (text.Length - at < stem.Length) return false;
            for (int k = 0; k < stem.Length - 1; k++)
                if (text[at + k] != stem[k]) return false;
            char last = stem[stem.Length - 1], here = text[at + stem.Length - 1];
            if (here == last) return true;
            if (!verb || !IsHangul(last) || !IsHangul(here)) return false;

            int a = last - HangulBase, b = here - HangulBase;
            int initialA = a / 588, vowelA = a % 588 / 28, finalA = a % 28;
            int initialB = b / 588, vowelB = b % 588 / 28, finalB = b % 28;
            if (initialA != initialB) return false;
            if (finalA == NoFinal && vowelA != vowelB)
                return Contracts(initialA, vowelA, vowelB) && (finalB == NoFinal || finalB == FinalSs);
            if (vowelA != vowelB) return false;
            switch (finalA)
            {
                case NoFinal: return finalB == FinalN || finalB == FinalL || finalB == FinalM || finalB == FinalB || finalB == FinalSs;
                case FinalL: return finalB == NoFinal || finalB == FinalN || finalB == FinalB;
                case FinalD: return finalB == FinalL;
                default: return false;
            }
        }

        // 줄기 끝 모음 + 어/아 → 한 글자: ㅣ→ㅕ · ㅗ→ㅘ · ㅜ→ㅝ · ㅚ→ㅙ · ㅡ→ㅓ/ㅏ · 하→해
        private static bool Contracts(int initial, int vowel, int into)
        {
            if (initial == InitialH && vowel == 0) return into == 1;
            switch (vowel)
            {
                case 20: return into == 6;
                case 8: return into == 9;
                case 13: return into == 14;
                case 11: return into == 10;
                case 18: return into == 4 || into == 0;
                default: return false;
            }
        }
    }
}
