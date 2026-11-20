using NUnit.Framework;
using WordRPG.Game;
using WordRPG.Words;

namespace WordRPG.Tests
{
    // 테스트 전체는 엔진 기본 방식(영단어RPG: 앞 지역 무료 + 정식판 인앱 결제, 처음에 단어장 고르기)으로 돈다 (Korean Word RPG #5).
    // 이 게임의 출시 설정(처음부터 유료 앱 · 단어장 고르기 끔)은 PaidAppTests가 테스트마다 켜서 따로 검사
    [SetUpFixture]
    public class SaleModeSetup
    {
        public static bool ShippedPaidApp { get; private set; }
        public static bool ShippedChoosable { get; private set; }

        [OneTimeSetUp]
        public void EngineMode()
        {
            ShippedPaidApp = Entitlements.PaidApp;
            ShippedChoosable = WordCourses.Choosable;
            Apply();
        }

        public static void Apply()
        {
            Entitlements.PaidApp = false;
            WordCourses.Choosable = true;
        }

        public static void Shipped()
        {
            Entitlements.PaidApp = ShippedPaidApp;
            WordCourses.Choosable = ShippedChoosable;
        }

        [OneTimeTearDown]
        public void Restore() => Shipped();
    }
}
