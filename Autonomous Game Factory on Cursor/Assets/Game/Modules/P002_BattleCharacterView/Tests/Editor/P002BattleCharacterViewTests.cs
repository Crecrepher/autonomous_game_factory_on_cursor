using NUnit.Framework;

namespace Game
{
    public class P002BattleCharacterViewTests
    {
        [Test]
        public void Create_ReturnsNonNull()
        {
            P002BattleCharacterViewRuntime runtime = new P002BattleCharacterViewRuntime();
            Assert.IsNotNull(runtime);
        }

        [Test]
        public void SetGaugeRatio_StoresValue()
        {
            P002BattleCharacterViewRuntime runtime = new P002BattleCharacterViewRuntime();
            runtime.Init(3);
            runtime.SetGaugeRatio(0, 0.5f);
            Assert.AreEqual(0.5f, runtime.GetGaugeRatio(0), 0.001f);
        }

        [Test]
        public void SetSkillReady_StoresValue()
        {
            P002BattleCharacterViewRuntime runtime = new P002BattleCharacterViewRuntime();
            runtime.Init(3);
            runtime.SetSkillReady(1, true);
            Assert.IsTrue(runtime.GetSkillReady(1));
        }
    }
}
