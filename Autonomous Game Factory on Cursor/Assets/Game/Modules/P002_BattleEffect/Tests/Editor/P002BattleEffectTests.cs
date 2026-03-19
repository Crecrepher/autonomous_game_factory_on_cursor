using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002BattleEffectTests
    {
        [Test]
        public void Create_ReturnsNonNullRuntime()
        {
            var config = ScriptableObject.CreateInstance<P002BattleEffectConfig>();
            var runtime = P002BattleEffectFactory.Create(config);
            Assert.IsNotNull(runtime);
        }

        [Test]
        public void Init_SetsPoolSize_ActiveCountZero()
        {
            var config = ScriptableObject.CreateInstance<P002BattleEffectConfig>();
            var runtime = P002BattleEffectFactory.Create(config) as P002BattleEffectRuntime;
            Assert.IsNotNull(runtime);

            Assert.AreEqual(0, runtime.ActiveProjectileCount);
        }

        [Test]
        public void LaunchProjectile_IncrementsActiveCount()
        {
            var config = ScriptableObject.CreateInstance<P002BattleEffectConfig>();
            var runtime = P002BattleEffectFactory.Create(config) as P002BattleEffectRuntime;
            Assert.IsNotNull(runtime);

            runtime.Init(5, 3, 6);
            runtime.SetProjectileSpeed(15f);
            runtime.LaunchProjectile((int)EP002ProjectileType.Sword, 0f, 0f, 1f, 0f, 0);

            Assert.AreEqual(1, runtime.ActiveProjectileCount);
        }

        [Test]
        public void Tick_AdvancesProjectileUntilArrived()
        {
            var config = ScriptableObject.CreateInstance<P002BattleEffectConfig>();
            var runtime = P002BattleEffectFactory.Create(config) as P002BattleEffectRuntime;
            Assert.IsNotNull(runtime);

            runtime.Init(5, 3, 6);
            runtime.SetProjectileSpeed(100f);
            runtime.LaunchProjectile((int)EP002ProjectileType.Sword, 0f, 0f, 1f, 0f, 0);

            var counter = new ArrivedCounter();
            runtime.OnProjectileArrived += counter.OnArrived;

            for (int i = 0; i < 100; i++)
            {
                runtime.Tick(0.1f);
                if (counter.Count > 0) break;
            }

            Assert.AreEqual(1, counter.Count);
            Assert.AreEqual(0, runtime.ActiveProjectileCount);
        }

        sealed class ArrivedCounter
        {
            public int Count;
            public void OnArrived(int idx) { Count++; }
        }
    }
}
