using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class BlacksmithTests
    {
        BlacksmithConfig _config;
        IBlacksmith _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BlacksmithConfig>();
            _runtime = BlacksmithFactory.CreateRuntime(_config);
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
            Assert.AreEqual(0, _runtime.ForgeCount);
            Assert.AreEqual(_config.MaxForgeCount, _runtime.MaxForgeCount);
            Assert.AreEqual(_config.ForgeCost, _runtime.ForgeCost);
        }

        [Test]
        public void TryForge_IncrementsCount()
        {
            _runtime.Init();
            bool result = _runtime.TryForge();
            Assert.IsTrue(result);
            Assert.AreEqual(1, _runtime.ForgeCount);
        }

        [Test]
        public void TryForge_MaxReached_ReturnsFalse()
        {
            _runtime.Init();
            for (int i = 0; i < _config.MaxForgeCount; i++)
            {
                _runtime.TryForge();
            }
            bool result = _runtime.TryForge();
            Assert.IsFalse(result);
        }

        [Test]
        public void OnForged_FiresOnForge()
        {
            _runtime.Init();
            _forgedFireCount = 0;
            _runtime.OnForged += HandleForged;
            _runtime.TryForge();
            Assert.AreEqual(1, _forgedFireCount);
            _runtime.OnForged -= HandleForged;
        }

        int _forgedFireCount;

        void HandleForged(int count)
        {
            _forgedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
