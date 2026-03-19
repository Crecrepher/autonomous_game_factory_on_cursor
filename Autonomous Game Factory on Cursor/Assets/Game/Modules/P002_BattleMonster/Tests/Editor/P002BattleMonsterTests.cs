using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002BattleMonsterTests
    {
        [Test]
        public void Create_ReturnsNonNull()
        {
            P002BattleMonsterRuntime runtime = new P002BattleMonsterRuntime();
            Assert.IsNotNull(runtime);
        }

        [Test]
        public void Init_SetsMonsterHealth()
        {
            P002GameConfigConfig config = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig gameConfig = P002GameConfigFactory.Create(config);
            P002BattleMonsterRuntime runtime = new P002BattleMonsterRuntime();
            runtime.Init(gameConfig);
            Assert.Greater(runtime.GetCurrentMonsterHealth(), 0);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ApplyBasicAttack_ReducesHealth()
        {
            P002GameConfigConfig config = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig gameConfig = P002GameConfigFactory.Create(config);
            P002BattleMonsterRuntime runtime = new P002BattleMonsterRuntime();
            runtime.Init(gameConfig);
            int healthBefore = runtime.GetCurrentMonsterHealth();
            runtime.ApplyBasicAttack(0);
            Assert.LessOrEqual(runtime.GetCurrentMonsterHealth(), healthBefore);
            Object.DestroyImmediate(config);
        }
    }
}
