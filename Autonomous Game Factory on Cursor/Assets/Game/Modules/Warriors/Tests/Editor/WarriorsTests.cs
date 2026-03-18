using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class WarriorsTests
    {
        WarriorsConfig _config;
        IWarriors _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<WarriorsConfig>();
            _runtime = WarriorsFactory.CreateRuntime(_config);
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
        public void Init_SetsDefaultValues()
        {
            _runtime.Init();
            Assert.AreEqual(0, _runtime.HiredCount);
            Assert.AreEqual(_config.MaxWarriors, _runtime.MaxWarriors);
        }

        [Test]
        public void TryHire_IncrementsCount()
        {
            _runtime.Init();
            bool result = _runtime.TryHire();
            Assert.IsTrue(result);
            Assert.AreEqual(1, _runtime.HiredCount);
        }

        [Test]
        public void TryHire_UpdatesCost()
        {
            _runtime.Init();
            int costBefore = _runtime.HireCost;
            _runtime.TryHire();
            Assert.AreNotEqual(costBefore, _runtime.HireCost);
        }

        [Test]
        public void OnWarriorHired_FiresOnHire()
        {
            _runtime.Init();
            _hiredFireCount = 0;
            _runtime.OnWarriorHired += HandleWarriorHired;
            _runtime.TryHire();
            Assert.AreEqual(1, _hiredFireCount);
            _runtime.OnWarriorHired -= HandleWarriorHired;
        }

        int _hiredFireCount;

        void HandleWarriorHired(int count)
        {
            _hiredFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
