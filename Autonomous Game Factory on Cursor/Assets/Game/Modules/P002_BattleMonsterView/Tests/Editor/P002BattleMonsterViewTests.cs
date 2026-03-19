using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002BattleMonsterViewTests
    {
        [Test]
        public void Create_ReturnsNonNullRuntime()
        {
            var config = ScriptableObject.CreateInstance<P002BattleMonsterViewConfig>();
            var runtime = P002BattleMonsterViewFactory.Create(config);
            Assert.IsNotNull(runtime);
        }

        [Test]
        public void Init_SetsMonsterCountAndHealthRatios()
        {
            var config = ScriptableObject.CreateInstance<P002BattleMonsterViewConfig>();
            var runtime = P002BattleMonsterViewFactory.Create(config) as P002BattleMonsterViewRuntime;
            Assert.IsNotNull(runtime);

            runtime.Init(2);

            Assert.AreEqual(2, runtime.MonsterCount);
            Assert.AreEqual(1f, runtime.GetHealthRatio(0));
            Assert.AreEqual(1f, runtime.GetHealthRatio(1));
        }

        [Test]
        public void UpdateHealth_CalculatesRatio()
        {
            var config = ScriptableObject.CreateInstance<P002BattleMonsterViewConfig>();
            var runtime = P002BattleMonsterViewFactory.Create(config) as P002BattleMonsterViewRuntime;
            Assert.IsNotNull(runtime);

            runtime.Init(1);
            runtime.UpdateHealth(0, 50, 100);

            Assert.AreEqual(0.5f, runtime.GetHealthRatio(0));
        }

        [Test]
        public void AddDamageToText_Accumulates()
        {
            var config = ScriptableObject.CreateInstance<P002BattleMonsterViewConfig>();
            var runtime = P002BattleMonsterViewFactory.Create(config) as P002BattleMonsterViewRuntime;
            Assert.IsNotNull(runtime);

            runtime.Init(1);
            runtime.ShowDamageText(10);
            runtime.AddDamageToText(5);

            Assert.AreEqual(15, runtime.AccumulatedDamage);
        }
    }
}
