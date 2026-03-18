using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class EconomyTests
    {
        EconomyConfig _config;
        IEconomy _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _runtime = EconomyFactory.CreateRuntime(_config);
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
        public void Init_SetsStartingBalance()
        {
            _runtime.Init();
            Assert.AreEqual(_config.StartingBalance, _runtime.Balance);
        }

        [Test]
        public void Add_IncreasesBalance()
        {
            _runtime.Init();
            int before = _runtime.Balance;
            _runtime.Add(10);
            Assert.AreEqual(before + 10, _runtime.Balance);
        }

        [Test]
        public void Add_ClampsToMaxBalance()
        {
            _runtime.Init();
            _runtime.Add(_config.MaxBalance + 100);
            Assert.AreEqual(_config.MaxBalance, _runtime.Balance);
        }

        [Test]
        public void TrySpend_WithSufficientBalance_ReturnsTrue()
        {
            _runtime.Init();
            _runtime.Add(100);
            int before = _runtime.Balance;
            bool result = _runtime.TrySpend(50);
            Assert.IsTrue(result);
            Assert.AreEqual(before - 50, _runtime.Balance);
        }

        [Test]
        public void TrySpend_WithInsufficientBalance_ReturnsFalse()
        {
            _runtime.Init();
            int before = _runtime.Balance;
            bool result = _runtime.TrySpend(before + 1);
            Assert.IsFalse(result);
            Assert.AreEqual(before, _runtime.Balance);
        }

        [Test]
        public void CanAfford_ReturnsCorrectResult()
        {
            _runtime.Init();
            Assert.IsTrue(_runtime.CanAfford(_runtime.Balance));
            Assert.IsTrue(_runtime.CanAfford(0));
            Assert.IsFalse(_runtime.CanAfford(_runtime.Balance + 1));
        }

        [Test]
        public void OnBalanceChanged_FiresOnAddAndSpend()
        {
            _runtime.Init();
            _balanceChangedFireCount = 0;
            _runtime.OnBalanceChanged += HandleBalanceChanged;

            _runtime.Add(10);
            Assert.AreEqual(1, _balanceChangedFireCount);

            _runtime.TrySpend(5);
            Assert.AreEqual(2, _balanceChangedFireCount);

            _runtime.OnBalanceChanged -= HandleBalanceChanged;
        }

        int _balanceChangedFireCount;

        void HandleBalanceChanged(int balance)
        {
            _balanceChangedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
