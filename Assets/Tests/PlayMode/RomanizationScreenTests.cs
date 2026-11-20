using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Heroes;
using WordRPG.Monsters;
using WordRPG.UI;
using WordRPG.Words;
using static WordRPG.Tests.UiDriver;
using Object = UnityEngine.Object;

namespace WordRPG.Tests
{
    // Korean Word RPG #8: 한국어 단어 아래 로마자 (Figma 'Korean Word RPG' 페이지) — 전투의 새 단어 카드·문제·한국어 보기·빈칸두꺼비
    public class RomanizationScreenTests
    {
        private static T Set<T>(T obj, string field, object value) where T : class
        {
            obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(obj, value);
            return obj;
        }

        private static EncounterTable Table(MonsterSpecies enemy)
        {
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            Set(table, "entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(enemy, 1, 1, 1) });
            Set(table, "minGroupSize", 1);
            Set(table, "maxGroupSize", 1);
            return table;
        }

        private static WordDatabase Korean()
        {
            var db = ScriptableObject.CreateInstance<WordDatabase>();
            db.ReplaceWords(new List<WordEntry>
            {
                new WordEntry("ko_hakgyo", "학교", "school", "n", romanization: "hakgyo"),
                new WordEntry("ko_saram", "사람", "person", "n", romanization: "saram"),
                new WordEntry("ko_gareuchida", "가르치다", "teach", "v", romanization: "gareuchida"),
                new WordEntry("ko_ijeon", "이전", "before, earlier", "n", romanization: "ijeon"),
                new WordEntry("ko_chaek", "책", "book", "n", romanization: "chaek"),
                new WordEntry("ko_bakda", "밝다", "bright, cheerful", "adj", romanization: "bakda"),
            });
            return db;
        }

        private static (BattleScreen screen, GameObject go) Battle(SkillData basic, MonsterSpecies enemy, WordDatabase words, Action<GameSession> prepare = null)
        {
            var hero = TestData.Hero(new MonsterStats(500, 30, 10)).Set("basicSkill", basic);
            var session = GameSession.NewGame(hero, 1);
            prepare?.Invoke(session);
            var go = new GameObject("BattleScreenUnderTest");
            var screen = go.AddComponent<BattleScreen>();
            screen.Configure(Table(enemy), words, session, animScale: 0.01f);
            return (screen, go);
        }

        private static MonsterSpecies Sturdy(SkillData skill) =>
            TestData.Species("dummy", new MonsterStats(999, 1, 50), new MonsterStats(0, 0, 0), skill);

        [UnityTest]
        public IEnumerator NewWordCardAndQuizShowTheRomanizationUnderTheKoreanWord()
        {
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 1);
            var (screen, go) = Battle(strike, Sturdy(strike), Korean());
            yield return null;
            yield return null;
            var root = screen.transform;
            yield return WaitFor(() =>
            {
                if (ActiveButton(root, "CardConfirmButton") != null) return true;
                (ActiveButton(root, "Intensity_0") ?? ActiveButton(root, "SkillButton_0"))?.onClick.Invoke();
                return false;
            }, 8f);
            var word = screen.Engine.CurrentQuestion.Word;
            Assert.AreEqual(word.Romanization, screen.ShownCardRoman, "새 단어 카드: 단어 아래 로마자");

            ActiveButton(root, "CardConfirmButton").onClick.Invoke();
            yield return WaitFor(() => ActiveButton(root, "Choice_0") != null, 5f);
            Assert.AreEqual(QuizDirection.EnglishToMeaning, screen.Engine.CurrentQuestion.Direction);
            Assert.AreEqual(word.English, screen.ShownPrompt);
            Assert.AreEqual(word.Romanization, screen.ShownPromptRoman, "문제 단어 아래 로마자");
            for (int i = 0; i < 4; i++)
                Assert.AreEqual("", screen.ShownChoiceRoman(i), "영어 뜻 보기엔 로마자 없음");
            Object.Destroy(go);
            yield return null;
        }

        // 뜻 → 한국어 단어 문제 (어려운 기술): 보기마다 단어 아래 로마자, 문제 글자(영어 뜻)엔 없음
        [UnityTest]
        public IEnumerator KoreanChoicesShowTheirRomanization()
        {
            var hard = TestData.Skill("hard", SkillKind.Damage, SkillTarget.SingleEnemy, 1).Set("quizDirection", QuizDirection.MeaningToEnglish);
            var words = Korean();
            var (screen, go) = Battle(hard, Sturdy(hard), words, session =>
            {
                foreach (var w in words.Words) session.Vocabulary.Discover(w.Id, DateTime.UtcNow.AddDays(-30)); // 새 단어는 늘 단어 → 뜻이라 모두 본 단어로
            });
            yield return null;
            yield return null;
            var root = screen.transform;
            yield return WaitFor(() =>
            {
                if (ActiveButton(root, "Choice_0") != null) return true;
                (ActiveButton(root, "CardConfirmButton") ?? ActiveButton(root, "Intensity_0") ?? ActiveButton(root, "SkillButton_0"))?.onClick.Invoke();
                return false;
            }, 8f);
            var question = screen.Engine.CurrentQuestion;
            Assert.AreEqual(QuizDirection.MeaningToEnglish, question.Direction);
            Assert.AreEqual("", screen.ShownPromptRoman, "문제 글자가 영어 뜻이면 로마자 줄 없음");
            for (int i = 0; i < question.Choices.Count; i++)
            {
                Assert.AreEqual(question.Choices[i], question.ChoiceWords[i].English);
                Assert.AreEqual(question.ChoiceWords[i].Romanization, screen.ShownChoiceRoman(i), $"보기 {i}: 단어 아래 로마자");
                Assert.IsNotEmpty(screen.ShownChoiceRoman(i));
            }
            Object.Destroy(go);
            yield return null;
        }

        // 빈칸두꺼비: 글자를 가린 동안은 로마자를 숨기고(답을 알려 주므로), 답하면 채운 단어와 함께 보임
        [UnityTest]
        public IEnumerator BlankedWordsHideTheRomanizationUntilAnswered()
        {
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 1);
            var croak = TestData.Skill("croak", SkillKind.Damage, SkillTarget.AllEnemies, 1).Set("displayName", "빈칸 울음");
            var frog = TestData.Species("frog", new MonsterStats(999, 1, 50), new MonsterStats(0, 0, 0), croak)
                .Set("displayName", "빈칸두꺼비").Set("gimmick", BossGimmick.Blank);
            var (screen, go) = Battle(strike, frog, Korean());
            yield return null;
            yield return null;
            var root = screen.transform;
            yield return WaitFor(() =>
            {
                if (ActiveButton(root, "Choice_0") != null) return true;
                (ActiveButton(root, "CardConfirmButton") ?? ActiveButton(root, "Intensity_0") ?? ActiveButton(root, "SkillButton_0"))?.onClick.Invoke();
                return false;
            }, 8f);
            Assert.IsTrue(screen.Engine.WordsBlanked);
            var word = screen.Engine.CurrentQuestion.Word;
            Assert.AreEqual("", screen.ShownPromptRoman, $"빈칸 동안 로마자 숨김 ({screen.ShownPrompt})");

            ActiveButton(root, "Choice_0").onClick.Invoke();
            yield return null;
            yield return null;
            Assert.AreEqual(word.Romanization, screen.ShownPromptRoman, "답하면 로마자도 보임");
            Object.Destroy(go);
            yield return null;
        }
    }
}
