using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class FortressTests
    {
        FortressConfig _config;
        IFortress _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<FortressConfig>();
            _runtime = FortressFactory.CreateRuntime(_config);
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
        public void Init_SetsMaxHp()
        {
            _runtime.Init();
            Assert.AreEqual(_config.MaxHp, _runtime.CurrentHp);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _runtime.Init();
            int before = _runtime.CurrentHp;
            _runtime.TakeDamage(10);
            Assert.AreEqual(before - 10, _runtime.CurrentHp);
        }

        [Test]
        public void TakeDamage_ClampsToZero()
        {
            _runtime.Init();
            _runtime.TakeDamage(_config.MaxHp + 100);
            Assert.AreEqual(0, _runtime.CurrentHp);
        }

        [Test]
        public void TryUpgrade_IncrementsLevel()
        {
            _runtime.Init();
            bool result = _runtime.TryUpgrade();
            Assert.IsTrue(result);
            Assert.AreEqual(1, _runtime.UpgradeLevel);
        }

        [Test]
        public void TryUpgrade_MaxLevel_ReturnsFalse()
        {
            _runtime.Init();
            _runtime.TryUpgrade();
            _runtime.TryUpgrade();
            bool result = _runtime.TryUpgrade();
            Assert.IsFalse(result);
        }

        [Test]
        public void GetUpgradeCost_ReturnsCorrectValues()
        {
            _runtime.Init();
            Assert.AreEqual(_config.Upgrade1Cost, _runtime.GetUpgradeCost(1));
            Assert.AreEqual(_config.Upgrade2Cost, _runtime.GetUpgradeCost(2));
            Assert.AreEqual(0, _runtime.GetUpgradeCost(3));
        }

        [Test]
        public void OnUpgraded_FiresOnUpgrade()
        {
            _runtime.Init();
            _upgradedFireCount = 0;
            _runtime.OnUpgraded += HandleUpgraded;
            _runtime.TryUpgrade();
            Assert.AreEqual(1, _upgradedFireCount);
            _runtime.OnUpgraded -= HandleUpgraded;
        }

        int _upgradedFireCount;

        void HandleUpgraded(int level)
        {
            _upgradedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
