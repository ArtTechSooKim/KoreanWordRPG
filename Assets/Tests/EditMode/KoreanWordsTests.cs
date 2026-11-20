using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using WordRPG.Game;
using WordRPG.Words;

namespace WordRPG.Tests
{
    // Korean Word RPG (#1): 한국어 단어가 예문 어디에 있는지 · 한국어 단어장 데이터
    public class KoreanWordsTests
    {
        private static int At(string sentence, string word, string pos) => KoreanWords.Find(sentence, word, pos).start;

        [Test]
        public void NounsKeepTheirLettersAndTakeParticles()
        {
            Assert.AreEqual(9, At("이 일기에는 내 이름이 제목으로", "이름", "n"));
            Assert.AreEqual(4, At("나는 \"집\"이라고 말했다.", "집", "n"), "따옴표 다음도 어절 첫 글자");
            Assert.AreEqual(-1, At("마을 한가운데 둥근 샘이 있다.", "새", "n"), "명사는 받침이 붙은 다른 글자(샘)와 다름");
            Assert.AreEqual(-1, At("나는 하루하루를 기록한다.", "루", "n"), "어절 가운데는 아님");
            Assert.AreEqual(0, At("새 법이 생겼다.", "새", "adj"), "관형사는 어미가 붙지 않음");
            Assert.AreEqual(-1, At("둥근 샘이 있다.", "새", "adj"));
        }

        [Test]
        public void VerbsMatchTheirStemThroughEndings()
        {
            Assert.AreEqual((6, 1), KoreanWords.Find("아이들에게 읽고 쓰는 법을", "읽다", "v"));
            Assert.AreEqual((13, 1), KoreanWords.Find("내 제자 목록은 해마다 길어진다.", "길다", "adj"), "줄기 그대로");
            Assert.AreEqual(0, At("가는 길", "가다", "v"), "받침이 붙음 (가 → 간·갈·갑·갔)");
            Assert.AreEqual(0, At("갔다", "가다", "v"));
            Assert.AreEqual(19, At("우리의 작은 공동체는 푸른 초원에 산다.", "살다", "v"), "ㄹ 탈락");
            Assert.AreEqual(14, At("나는 또렷한 단어 하나를 들었다.", "듣다", "v"), "ㄷ 불규칙");
        }

        [Test]
        public void ContractedEndingsStillCount()
        {
            Assert.GreaterOrEqual(At("글자도 모두 사라져 있었다", "사라지다", "v"), 0, "지 + 어 → 져");
            Assert.GreaterOrEqual(At("학자들이 모여 서고를 세웠다.", "모이다", "v"), 0, "이 + 어 → 여");
            Assert.GreaterOrEqual(At("생각들을 모아 배를 만들었다.", "모으다", "v"), 0, "으 + 아 → 아");
            Assert.GreaterOrEqual(At("주전자가 노래했다.", "노래하다", "v"), 0, "하 + 였 → 했");
            Assert.AreEqual(-1, At("우리는 걸었다.", "가다", "v"), "첫 소리가 다르면 아님");
        }

        [Test]
        public void EnglishWordsStillMatchWholeWords()
        {
            Assert.AreEqual(10, Journals.WordSpan("I was not able to say it.", "able").start);
            Assert.AreEqual(-1, Journals.WordSpan("I was unable to say it.", "able").start);
        }

        // #8: 문제마다 한국어 쪽의 로마자 — 단어 → 뜻이면 문제 단어, 뜻 → 단어면 보기마다 (화면은 그 아래 로마자 한 줄)
        [Test]
        public void QuizQuestionsKnowTheRomanizationOfTheirKoreanSide()
        {
            var pool = new List<WordEntry>
            {
                new WordEntry("ko_hakgyo", "학교", "school", "n", romanization: "hakgyo"),
                new WordEntry("ko_saram", "사람", "person", "n", romanization: "saram"),
                new WordEntry("ko_ijeon", "이전", "before, earlier", "n", romanization: "ijeon"),
                new WordEntry("ko_chaek", "책", "book", "n", romanization: "chaek"),
                new WordEntry("ko_bam", "밤", "night", "n", romanization: "bam"),
            };
            var target = pool[2];
            for (int seed = 0; seed < 20; seed++)
            {
                var toMeaning = QuizGenerator.Create(target, QuizDirection.EnglishToMeaning, pool, new System.Random(seed));
                Assert.AreEqual("ijeon", toMeaning.PromptRomanization);
                for (int i = 0; i < toMeaning.Choices.Count; i++)
                {
                    Assert.AreEqual(toMeaning.ChoiceWords[i].Meaning, toMeaning.Choices[i], "보기와 같은 순서");
                    Assert.AreEqual("", toMeaning.ChoiceRomanization(i), "영어 뜻 보기엔 없음");
                }

                var toWord = QuizGenerator.Create(target, QuizDirection.MeaningToEnglish, pool, new System.Random(seed));
                Assert.AreEqual("", toWord.PromptRomanization, "문제 글자가 영어 뜻이면 없음");
                Assert.AreSame(target, toWord.ChoiceWords[toWord.CorrectIndex]);
                for (int i = 0; i < toWord.Choices.Count; i++)
                {
                    Assert.AreEqual(toWord.ChoiceWords[i].English, toWord.Choices[i]);
                    Assert.AreEqual(toWord.ChoiceWords[i].Romanization, toWord.ChoiceRomanization(i));
                }
            }
            // 보기 단어를 모르는 문제(테스트·시뮬레이션용 생성자)는 로마자 없음
            var bare = new QuizQuestion(target, QuizDirection.MeaningToEnglish, false, "before", new[] { "이전", "학교" }, 0);
            Assert.AreEqual("", bare.ChoiceRomanization(0));
            Assert.AreEqual("", bare.ChoiceRomanization(5));
        }

        // 기본 단어장 = 한국어 단어 + 영어 뜻 + 로마자 (단어장 고르기는 끔 — 이 두 권이 게임의 단어 전부)
        [TestCase("Assets/Data/Words/tier1_common.asset", 150)]
        [TestCase("Assets/Data/Words/tier2_advanced.asset", 300)]
        public void BaseBooksAreKoreanWordsWithEnglishMeaningsAndRomanization(string path, int count)
        {
            var book = AssetDatabase.LoadAssetAtPath<WordDatabase>(path);
            Assert.IsNotNull(book, path);
            Assert.AreEqual(count, book.OwnWords.Count);
            foreach (var word in book.OwnWords)
            {
                StringAssert.StartsWith("ko_", word.Id);
                Assert.IsTrue(KoreanWords.HasHangul(word.English), $"{word.Id}: 한국어 단어 ({word.English})");
                Assert.IsFalse(KoreanWords.HasHangul(word.Meaning), $"{word.Id}: 뜻은 영어 ({word.Meaning})");
                foreach (var part in word.GetMeaningParts()) // 동사도 'to' 없이 (#3 사용자 — 보기 버튼이 짧게)
                    Assert.IsFalse(part.StartsWith("to "), $"{word.Id}: '{part}' 앞의 to");
                Assert.IsFalse(KoreanWords.HasHangul(word.ExampleMeaning), $"{word.Id}: 해석은 영어");
                Assert.IsTrue(Regex.IsMatch(word.Romanization, "^[a-z]+$"), $"{word.Id}: 로마자 ({word.Romanization})");
            }
        }
    }
}
