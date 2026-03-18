using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class UITests
    {
        UIConfig _config;
        IUI _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<UIConfig>();
            _runtime = UIFactory.CreateRuntime(_config);
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
        public void Show_SetsVisible()
        {
            _runtime.Init();
            _runtime.Show();
            Assert.IsTrue(_runtime.IsVisible);
        }

        [Test]
        public void Hide_ClearsVisible()
        {
            _runtime.Init();
            _runtime.Show();
            _runtime.Hide();
            Assert.IsFalse(_runtime.IsVisible);
        }

        [Test]
        public void UpdateCoinDisplay_StoresValue()
        {
            _runtime.Init();
            _runtime.UpdateCoinDisplay(100);
            UIRuntime concrete = (UIRuntime)_runtime;
            Assert.AreEqual(100, concrete.GetDisplayedCoinAmount());
        }

        [Test]
        public void ShowCTA_HideCTA_TogglesState()
        {
            _runtime.Init();
            _runtime.ShowCTA();
            UIRuntime concrete = (UIRuntime)_runtime;
            Assert.IsTrue(concrete.IsCTAVisible());
            _runtime.HideCTA();
            Assert.IsFalse(concrete.IsCTAVisible());
        }

        [Test]
        public void OnCTAClicked_FiresOnClick()
        {
            _runtime.Init();
            _ctaClickCount = 0;
            _runtime.OnCTAClicked += HandleCTAClicked;
            UIRuntime concrete = (UIRuntime)_runtime;
            concrete.SimulateCTAClick();
            Assert.AreEqual(1, _ctaClickCount);
            _runtime.OnCTAClicked -= HandleCTAClicked;
        }

        int _ctaClickCount;

        void HandleCTAClicked()
        {
            _ctaClickCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
