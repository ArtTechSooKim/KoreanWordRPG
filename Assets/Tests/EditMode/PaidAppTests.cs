using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordRPG.Game;
using WordRPG.Heroes;
using WordRPG.Save;
using WordRPG.Words;

namespace WordRPG.Tests
{
    // 이 게임의 출시 설정 (#5 사용자): 처음부터 유료 앱 1,000원 — 누구나 정식판 · 단어장 고르기 끔 (모두 기본 = 한국어 단어장)
    public class PaidAppTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void ShippedMode() => SaleModeSetup.Shipped();

        [TearDown]
        public void EngineMode()
        {
            SaleModeSetup.Apply();
            WordCourses.Active = null;
        }

        [Test]
        public void ThisGameShipsAsAPaidAppWithoutChoosingWords()
        {
            Assert.IsTrue(SaleModeSetup.ShippedPaidApp, "처음부터 유료 앱 (#5 사용자)");
            Assert.IsFalse(SaleModeSetup.ShippedChoosable, "단어장 고르기 끔 (#5 사용자)");
        }

        [Test]
        public void EveryoneHasTheFullVersion()
        {
            Assert.IsTrue(new Entitlements().HasFullVersion);
            Assert.IsTrue(Entitlements.FromStorage("").HasFullVersion, "기기에 산 기록이 없어도");
        }

        [Test]
        public void NewGamesUseTheBasicKoreanWordsWithoutAsking()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroData>("Assets/Data/Hero/hero.asset");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            var session = GameSession.NewGame(hero, 1);
            Assert.IsFalse(session.NeedsCourseChoice, "고르는 화면 없음");

            var data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(session.ToSaveData(Now)));
            var loaded = GameSession.FromSaveData(data, database, hero);
            Assert.IsFalse(loaded.NeedsCourseChoice);
            Assert.AreEqual(WordCourses.DefaultId, loaded.CourseId);
            Assert.AreEqual(0, loaded.LoadWarnings.Count);
            StringAssert.StartsWith("ko_", database.FindArea("meadow").Words.Words[0].Id, "기본 단어장 = 한국어 단어");
        }
    }
}
