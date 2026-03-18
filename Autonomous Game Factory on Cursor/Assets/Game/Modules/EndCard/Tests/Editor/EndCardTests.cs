using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class EndCardTests
    {
        EndCardConfig _config;
        IEndCard _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EndCardConfig>();
            _runtime = EndCardFactory.CreateRuntime(_config);
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
        public void Init_SetsInvisible()
        {
            _runtime.Init();
            Assert.IsFalse(_runtime.IsVisible);
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
        public void OnShown_FiresOnShow()
        {
            _runtime.Init();
            _shownFireCount = 0;
            _runtime.OnShown += HandleShown;
            _runtime.Show();
            Assert.AreEqual(1, _shownFireCount);
            _runtime.OnShown -= HandleShown;
        }

        [Test]
        public void OnCTAClicked_FiresOnClick()
        {
            _runtime.Init();
            _ctaClickCount = 0;
            _runtime.OnCTAClicked += HandleCTAClicked;
            EndCardRuntime concrete = (EndCardRuntime)_runtime;
            concrete.SimulateCTAClick();
            Assert.AreEqual(1, _ctaClickCount);
            _runtime.OnCTAClicked -= HandleCTAClicked;
        }

        int _shownFireCount;
        int _ctaClickCount;

        void HandleShown()
        {
            _shownFireCount++;
        }

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
