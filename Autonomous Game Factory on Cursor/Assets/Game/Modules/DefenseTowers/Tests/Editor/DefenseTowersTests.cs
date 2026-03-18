using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class DefenseTowersTests
    {
        DefenseTowersConfig _config;
        IDefenseTowers _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DefenseTowersConfig>();
            _runtime = DefenseTowersFactory.CreateRuntime(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
            _runtime = null;
        }

        [Test]
        public void CreateRuntime_WithConfig_ReturnsNonNull()
        {
            Assert.IsNotNull(_runtime);
        }

        [Test]
        public void Init_ThenTick_DoesNotThrow()
        {
            _runtime.Init();
            Assert.DoesNotThrow(TickOnce);
        }

        [Test]
        public void Init_SetsDefaults()
        {
            _runtime.Init();
            Assert.AreEqual(0, _runtime.BuiltCount);
            Assert.AreEqual(_config.MaxTowers, _runtime.MaxTowers);
        }

        [Test]
        public void TryBuild_BuildsTower()
        {
            _runtime.Init();
            bool result = _runtime.TryBuild(0);
            Assert.IsTrue(result);
            Assert.AreEqual(1, _runtime.BuiltCount);
            Assert.IsTrue(_runtime.IsTowerBuilt(0));
        }

        [Test]
        public void TryBuild_AlreadyBuilt_ReturnsFalse()
        {
            _runtime.Init();
            _runtime.TryBuild(0);
            bool result = _runtime.TryBuild(0);
            Assert.IsFalse(result);
        }

        [Test]
        public void GetBuildCost_EarlyTower_ReturnsEarlyCost()
        {
            _runtime.Init();
            Assert.AreEqual(_config.EarlyCost, _runtime.GetBuildCost(0));
        }

        [Test]
        public void GetBuildCost_LateTower_ReturnsLateCost()
        {
            _runtime.Init();
            Assert.AreEqual(_config.LateCost, _runtime.GetBuildCost(_config.EarlyTowerCount));
        }

        [Test]
        public void OnTowerBuilt_FiresOnBuild()
        {
            _runtime.Init();
            _builtFireCount = 0;
            _runtime.OnTowerBuilt += HandleTowerBuilt;
            _runtime.TryBuild(0);
            Assert.AreEqual(1, _builtFireCount);
            _runtime.OnTowerBuilt -= HandleTowerBuilt;
        }

        int _builtFireCount;

        void HandleTowerBuilt(int index)
        {
            _builtFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
