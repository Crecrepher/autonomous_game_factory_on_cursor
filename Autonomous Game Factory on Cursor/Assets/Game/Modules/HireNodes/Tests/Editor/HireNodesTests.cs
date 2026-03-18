using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class HireNodesTests
    {
        HireNodesConfig _config;
        IHireNodes _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<HireNodesConfig>();
            _runtime = HireNodesFactory.CreateRuntime(_config);
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
        public void ActivateNode_IncreasesActiveCount()
        {
            _runtime.Init();
            _runtime.ActivateNode(0);
            Assert.AreEqual(1, _runtime.ActiveNodeCount);
            Assert.IsTrue(_runtime.IsNodeActive(0));
        }

        [Test]
        public void DeactivateNode_DecreasesActiveCount()
        {
            _runtime.Init();
            _runtime.ActivateNode(0);
            _runtime.DeactivateNode(0);
            Assert.AreEqual(0, _runtime.ActiveNodeCount);
            Assert.IsFalse(_runtime.IsNodeActive(0));
        }

        [Test]
        public void OnNodeActivated_FiresOnActivate()
        {
            _runtime.Init();
            _activatedFireCount = 0;
            _runtime.OnNodeActivated += HandleNodeActivated;
            _runtime.ActivateNode(0);
            Assert.AreEqual(1, _activatedFireCount);
            _runtime.OnNodeActivated -= HandleNodeActivated;
        }

        int _activatedFireCount;

        void HandleNodeActivated(int index)
        {
            _activatedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
