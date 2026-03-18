using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class PickupsTests
    {
        PickupsConfig _config;
        IPickups _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PickupsConfig>();
            _runtime = PickupsFactory.CreateRuntime(_config);
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
        public void SpawnPickup_IncreasesPendingCount()
        {
            _runtime.Init();
            _runtime.SpawnPickup(1);
            Assert.AreEqual(1, _runtime.PendingCount);
        }

        [Test]
        public void CollectAll_ClearsPending()
        {
            _runtime.Init();
            _runtime.SpawnPickup(1);
            _runtime.SpawnPickup(5);
            _runtime.CollectAll();
            Assert.AreEqual(0, _runtime.PendingCount);
            Assert.AreEqual(2, _runtime.TotalCollected);
        }

        [Test]
        public void OnPickupCollected_FiresOnCollect()
        {
            _runtime.Init();
            _runtime.SpawnPickup(10);
            _collectedFireCount = 0;
            _lastCollectedValue = 0;
            _runtime.OnPickupCollected += HandlePickupCollected;
            _runtime.CollectAll();
            Assert.AreEqual(1, _collectedFireCount);
            Assert.AreEqual(10, _lastCollectedValue);
            _runtime.OnPickupCollected -= HandlePickupCollected;
        }

        int _collectedFireCount;
        int _lastCollectedValue;

        void HandlePickupCollected(int value)
        {
            _collectedFireCount++;
            _lastCollectedValue = value;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
