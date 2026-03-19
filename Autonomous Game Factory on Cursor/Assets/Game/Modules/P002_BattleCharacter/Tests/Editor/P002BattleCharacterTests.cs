using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002BattleCharacterTests
    {
        [Test]
        public void Create_ReturnsNonNull()
        {
            P002BattleCharacterConfig config = ScriptableObject.CreateInstance<P002BattleCharacterConfig>();
            IP002BattleCharacter runtime = P002BattleCharacterFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AddGauge_IncreasesRatio()
        {
            P002BattleCharacterConfig config = ScriptableObject.CreateInstance<P002BattleCharacterConfig>();
            P002GameConfigConfig gameConfigConfig = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig gameConfig = P002GameConfigFactory.Create(gameConfigConfig);

            IP002BattleCharacter runtime = P002BattleCharacterFactory.Create(config);
            runtime.Init(gameConfig);

            float ratioBefore = runtime.GetSkillGaugeRatio(0);
            runtime.AddGauge(0, 3);
            float ratioAfter = runtime.GetSkillGaugeRatio(0);

            Assert.GreaterOrEqual(ratioAfter, ratioBefore);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(gameConfigConfig);
        }

        [Test]
        public void ResetGauge_ZeroesRatio()
        {
            P002BattleCharacterConfig config = ScriptableObject.CreateInstance<P002BattleCharacterConfig>();
            P002GameConfigConfig gameConfigConfig = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig gameConfig = P002GameConfigFactory.Create(gameConfigConfig);

            IP002BattleCharacter runtime = P002BattleCharacterFactory.Create(config);
            runtime.Init(gameConfig);
            runtime.AddGauge(0, 2);
            runtime.ResetGauge(0);

            float ratio = runtime.GetSkillGaugeRatio(0);
            Assert.AreEqual(0f, ratio);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(gameConfigConfig);
        }
    }
}
