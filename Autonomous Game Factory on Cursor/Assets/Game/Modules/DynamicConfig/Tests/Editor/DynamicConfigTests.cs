using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class DynamicConfigTests
    {
        DynamicConfigConfig _config;
        IDynamicConfig _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DynamicConfigConfig>();
            _runtime = DynamicConfigFactory.CreateRuntime(_config);
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
        public void SetFloat_GetFloat_ReturnsValue()
        {
            _runtime.Init();
            _runtime.SetFloat(1, 3.14f);
            Assert.AreEqual(3.14f, _runtime.GetFloat(1, 0f));
        }

        [Test]
        public void SetInt_GetInt_ReturnsValue()
        {
            _runtime.Init();
            _runtime.SetInt(2, 42);
            Assert.AreEqual(42, _runtime.GetInt(2, 0));
        }

        [Test]
        public void GetFloat_WithNoKey_ReturnsDefault()
        {
            _runtime.Init();
            Assert.AreEqual(9.99f, _runtime.GetFloat(999, 9.99f));
        }

        [Test]
        public void GetInt_WithNoKey_ReturnsDefault()
        {
            _runtime.Init();
            Assert.AreEqual(7, _runtime.GetInt(999, 7));
        }

        [Test]
        public void HasKey_ReturnsTrueAfterSet()
        {
            _runtime.Init();
            Assert.IsFalse(_runtime.HasKey(1));
            _runtime.SetFloat(1, 1f);
            Assert.IsTrue(_runtime.HasKey(1));
        }

        [Test]
        public void OnValueChanged_FiresOnSet()
        {
            _runtime.Init();
            _valueChangedFireCount = 0;
            _runtime.OnValueChanged += HandleValueChanged;
            _runtime.SetFloat(1, 1f);
            Assert.AreEqual(1, _valueChangedFireCount);
            _runtime.SetInt(2, 2);
            Assert.AreEqual(2, _valueChangedFireCount);
            _runtime.OnValueChanged -= HandleValueChanged;
        }

        int _valueChangedFireCount;

        void HandleValueChanged(int key)
        {
            _valueChangedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
