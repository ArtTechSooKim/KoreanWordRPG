#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using WordRPG.Battle;
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
    // Korean Word RPG #8: 로마자 화면 캡처 — StoreScreenshots에 없는 화면 (어려운 문제의 한국어 보기 · 사전 말풍선 · 반딧돌 · 석판 · 선제공격).
    // 평소엔 돌지 않음 ([Explicit]). 그래픽이 있어야 하므로 -nographics 없이: -testFilter WordRPG.Tests.RomanizationShots → Builds/store/raw/k*.png
    [Explicit]
    public class RomanizationShots
    {
        private static void Set(object obj, string field, object value) =>
            obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(obj, value);

        [UnityTest]
        public IEnumerator Views()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            var heroData = AssetDatabase.LoadAssetAtPath<HeroData>("Assets/Data/Hero/hero.asset");
            var meadow = db.FindArea("meadow");
            var words = meadow.Words.Words;
            var cam = StoreScreenshots.MakeCamera();
            var session = StoreScreenshots.Session(heroData, db, 8);
            session.World.SetPosition("meadow", new Vector2Int(22, 25));
            var field = StoreScreenshots.Field(meadow, session, db);
            yield return new WaitForSecondsRealtime(0.6f);

            // 사전 말풍선: 단어 → 로마자 → 뜻
            var school = words[8];
            var bubble = StoreScreenshots.Private<SpeechBubble>(field, "bubble");
            bubble.ShowWord("새로운 단어를 발견했다!", school.English, school.Meaning, $"{meadow.Words.RegionName} 도감 9 / {words.Count}", 10f, school.Romanization);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return StoreScreenshots.Shot("k1_bubble");
            bubble.Hide();

            // 반딧돌: 떠오른 글자 → 로마자 (단어 → 뜻)
            var stone = StoreScreenshots.Private<GlowStoneQuizView>(field, "stoneView");
            stone.Show(QuizGenerator.Create(words[2], QuizDirection.EnglishToMeaning, words, new System.Random(3)), 2, 7, _ => { }, () => { });
            yield return new WaitForSecondsRealtime(0.4f);
            yield return StoreScreenshots.Shot("k2_glowstone");
            stone.Hide();

            // 석판: 영어 뜻 → 한국어 보기 + 로마자 (가장 긴 단어가 섞이게)
            var tablet = StoreScreenshots.Private<TabletQuizView>(field, "tabletView");
            tablet.Show(1, 4, _ => { }, () => { });
            tablet.ShowQuestion(QuizGenerator.Create(words[2], QuizDirection.MeaningToEnglish, words, new System.Random(5)), 0, "틀려도 괜찮아요");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return StoreScreenshots.Shot("k3_tablet");
            tablet.Hide();

            // 선제공격: 단어 → 로마자
            var preempt = StoreScreenshots.Private<PreemptView>(field, "preemptView");
            preempt.Show(QuizGenerator.Create(words[13], QuizDirection.EnglishToMeaning, words, new System.Random(7)), 5f, _ => { });
            for (int i = 0; i < 20; i++) { preempt.Tick(0.02f); yield return null; }
            yield return StoreScreenshots.Shot("k4_preempt");
            Object.Destroy(field.gameObject);
            yield return new WaitForSecondsRealtime(0.3f);

            // 어려운 기술: 영어 뜻 → 한국어 보기 4개 + 로마자 (모든 단어를 본 상태 — 새 단어는 늘 단어 → 뜻)
            var hard = TestData.Skill("hard", SkillKind.Damage, SkillTarget.SingleEnemy, 1);
            Set(hard, "quizDirection", QuizDirection.MeaningToEnglish);
            var hero = TestData.Hero(new MonsterStats(500, 30, 10));
            Set(hero, "basicSkill", hard);
            var battleSession = GameSession.NewGame(hero, 10);
            foreach (var w in words) battleSession.Vocabulary.Discover(w.Id, DateTime.UtcNow.AddDays(-30));
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            Set(table, "entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(db.FindMonster("ink_slime"), 10, 10, 1) });
            Set(table, "minGroupSize", 1);
            Set(table, "maxGroupSize", 1);
            var go = new GameObject("RomanBattle");
            var screen = go.AddComponent<BattleScreen>();
            screen.Configure(table, meadow.Words, battleSession, animScale: 0.2f);
            var root = screen.transform;
            yield return WaitFor(() =>
            {
                if (ActiveButton(root, "Choice_0") != null) return true;
                (ActiveButton(root, "CardConfirmButton") ?? ActiveButton(root, "Intensity_0") ?? ActiveButton(root, "SkillButton_0"))?.onClick.Invoke();
                return false;
            }, 15f);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return StoreScreenshots.Shot("k5_hard_quiz");
            Object.Destroy(go);
            Object.Destroy(cam.gameObject);
        }
    }
}
#endif
