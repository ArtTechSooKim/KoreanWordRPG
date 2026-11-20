using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Heroes;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.UI;
using WordRPG.Words;
using static WordRPG.Tests.UiDriver;
using Object = UnityEngine.Object;

namespace WordRPG.Tests
{
    // 유료 앱 (#5 사용자: 처음부터 1,000원): 정식판 지역 입구에서 안내 없이 바로 들어가고, 설정에 정식판 칸이 없고, 특전도 없음
    public class PaidAppScreenTests
    {
        [SetUp]
        public void ShippedMode() => SaleModeSetup.Shipped();

        [TearDown]
        public void EngineMode() => SaleModeSetup.Apply();

        [UnityTest]
        public IEnumerator PaidRegionsOpenWithoutAPaywallOrGift()
        {
            var bite = TestData.Skill("bite", SkillKind.Damage, SkillTarget.SingleEnemy, 3);
            var owl = TestData.Species("owl", new MonsterStats(30, 5, 5), new MonsterStats(0, 0, 0), bite).Set("displayName", "물음표부엉이");
            var table = ScriptableObject.CreateInstance<EncounterTable>()
                .Set("entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(owl, 1, 1, 1) });
            var words = ScriptableObject.CreateInstance<WordDatabase>().Set("regionId", "test").Set("regionName", "테스트");
            words.ReplaceWords(TestData.SampleWords());
            var home = ScriptableObject.CreateInstance<FieldArea>()
                .Set("areaId", "home").Set("displayName", "초원").Set("map", "######\n#P..D#\n######").Set("encounters", table).Set("words", words);
            var paid = ScriptableObject.CreateInstance<FieldArea>()
                .Set("areaId", "paid").Set("displayName", "숲").Set("map", "######\n#D..P#\n######").Set("encounters", table).Set("words", words)
                .Set("requiresFullVersion", true).Set("storyOrder", 3);
            home.Set("exits", new List<AreaExit> { new AreaExit(paid, 0) });
            paid.Set("exits", new List<AreaExit> { new AreaExit(home, 0) });
            var curious = TestData.Item("potion_curious").Set("displayName", "호기심 물약");
            var database = ScriptableObject.CreateInstance<GameDatabase>();
            database.ReplaceContents(new MonsterSpecies[0], new[] { curious }, new[] { home, paid });
            database.Set("fullVersionGift", new List<ItemGift> { new ItemGift(curious, 3) });
            var session = GameSession.NewGame(TestData.Hero(new MonsterStats(100, 10, 10)).Set("basicSkill",
                TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 5)), 1);

            var field = new GameObject("PaidAppField").AddComponent<FieldScreen>();
            field.Configure(home, session, step: 0.05f, animScale: 0.01f, gameDatabase: database, purchaseStore: null, owned: new Entitlements());
            yield return null;
            yield return null;
            Assert.AreEqual(0, field.FullVersionGiftsGiven, "특전 없음 (#5 사용자)");
            Assert.AreEqual(0, session.Inventory.GetCount(curious));
            Assert.IsTrue(field.CanChangeLook, "모습 바꾸기는 누구나");

            var hud = field.transform.Find("FieldHud/SafeArea");
            FindButton(hud, "SettingsButton").onClick.Invoke();
            yield return null;
            Assert.IsFalse(hud.GetComponentsInChildren<Button>(true).Any(b => b.name == "FullVersionOfferButton" && b.gameObject.activeInHierarchy),
                "설정에 정식판 칸 없음");
            field.HandleBack();
            yield return null;

            yield return HoldStick(field, Direction.Right, () => field.CurrentArea == paid || field.Paywall.IsOpen, 5f);
            Assert.IsFalse(field.Paywall.IsOpen, "정식판 안내 없음");
            yield return WaitFor(() => field.CurrentArea == paid, 3f);
            Assert.AreSame(paid, field.CurrentArea, "바로 들어감");
            Object.Destroy(field.gameObject);
            yield return null;
        }
    }
}
