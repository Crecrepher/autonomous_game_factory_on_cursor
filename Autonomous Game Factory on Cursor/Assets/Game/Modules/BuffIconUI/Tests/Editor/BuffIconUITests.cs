using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class BuffIconUITests
    {
        BuffIconUIConfig _config;
        IBuffIconUI _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BuffIconUIConfig>();
            _runtime = BuffIconUIFactory.CreateRuntime(_config);
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
        public void AddIcon_IncreasesCount()
        {
            _runtime.Init();
            Assert.AreEqual(0, _runtime.ActiveIconCount);
            _runtime.AddIcon(1, 5f);
            Assert.AreEqual(1, _runtime.ActiveIconCount);
        }

        [Test]
        public void RemoveIcon_DecreasesCount()
        {
            _runtime.Init();
            _runtime.AddIcon(1, 5f);
            _runtime.RemoveIcon(1);
            Assert.AreEqual(0, _runtime.ActiveIconCount);
        }

        [Test]
        public void ClearAll_ResetsCount()
        {
            _runtime.Init();
            _runtime.AddIcon(1, 5f);
            _runtime.AddIcon(2, 5f);
            _runtime.AddIcon(3, 5f);
            _runtime.ClearAll();
            Assert.AreEqual(0, _runtime.ActiveIconCount);
        }

        [Test]
        public void Tick_ExpiresIcon_WhenDurationEnds()
        {
            _runtime.Init();
            _runtime.AddIcon(1, 0.5f);
            Assert.AreEqual(1, _runtime.ActiveIconCount);
            _runtime.Tick(1f);
            Assert.AreEqual(0, _runtime.ActiveIconCount);
        }

        [Test]
        public void AddIcon_SameId_RefreshesDuration()
        {
            _runtime.Init();
            _runtime.AddIcon(1, 1f);
            _runtime.AddIcon(1, 5f);
            Assert.AreEqual(1, _runtime.ActiveIconCount);
        }

        [Test]
        public void OnIconAdded_FiresOnAdd()
        {
            _runtime.Init();
            _iconAddedFireCount = 0;
            _runtime.OnIconAdded += HandleIconAdded;
            _runtime.AddIcon(1, 5f);
            Assert.AreEqual(1, _iconAddedFireCount);
            _runtime.OnIconAdded -= HandleIconAdded;
        }

        [Test]
        public void OnIconRemoved_FiresOnRemove()
        {
            _runtime.Init();
            _runtime.AddIcon(1, 5f);
            _iconRemovedFireCount = 0;
            _runtime.OnIconRemoved += HandleIconRemoved;
            _runtime.RemoveIcon(1);
            Assert.AreEqual(1, _iconRemovedFireCount);
            _runtime.OnIconRemoved -= HandleIconRemoved;
        }

        int _iconAddedFireCount;
        int _iconRemovedFireCount;

        void HandleIconAdded(int effectId)
        {
            _iconAddedFireCount++;
        }

        void HandleIconRemoved(int effectId)
        {
            _iconRemovedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
