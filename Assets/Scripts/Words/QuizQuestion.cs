using System.Collections.Generic;

namespace WordRPG.Words
{
    public enum QuizDirection
    {
        EnglishToMeaning, // "abandon" → 뜻 고르기 (쉬움)
        MeaningToEnglish  // "버리다, 포기하다" → 영단어 고르기 (어려움)
    }

    public class QuizQuestion
    {
        public WordEntry Word { get; }
        public QuizDirection Direction { get; }
        public bool IsNewWord { get; } // true면 UI가 문제 전에 '새 단어' 카드부터 보여준다
        public string Prompt { get; }
        public IReadOnlyList<string> Choices { get; }
        public int CorrectIndex { get; }

        public string CorrectAnswer => Choices[CorrectIndex];

        // 보기마다 그 단어 (Choices와 같은 순서, 없으면 null) — 한국어 보기 아래 로마자를 보여 주려고 (Korean Word RPG #8)
        public IReadOnlyList<WordEntry> ChoiceWords { get; }

        public QuizQuestion(WordEntry word, QuizDirection direction, bool isNewWord, string prompt,
            IReadOnlyList<string> choices, int correctIndex, IReadOnlyList<WordEntry> choiceWords = null)
        {
            Word = word;
            Direction = direction;
            IsNewWord = isNewWord;
            Prompt = prompt;
            Choices = choices;
            CorrectIndex = correctIndex;
            ChoiceWords = choiceWords;
        }

        // 문제 글자(단어)의 로마자 — 단어 → 뜻 문제일 때만, 뜻 → 단어 문제는 문제 글자가 영어 뜻이라 없음
        public string PromptRomanization => Direction == QuizDirection.EnglishToMeaning ? Word?.Romanization ?? "" : "";

        // 보기의 로마자 — 뜻 → 단어 문제일 때만 (보기가 한국어 단어)
        public string ChoiceRomanization(int index)
        {
            if (Direction != QuizDirection.MeaningToEnglish || ChoiceWords == null || index < 0 || index >= ChoiceWords.Count) return "";
            return ChoiceWords[index]?.Romanization ?? "";
        }

        public bool IsCorrect(int choiceIndex) => choiceIndex == CorrectIndex;
    }
}
