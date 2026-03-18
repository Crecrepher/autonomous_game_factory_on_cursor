using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class GuideTests
    {
        GuideConfig _config;
        IGuide _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GuideConfig>();
            _runtime = GuideFactory.CreateRuntime(_config);
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
        public void ShowGuide_ActivatesGuide()
        {
            _runtime.Init();
            _runtime.ShowGuide(1);
            Assert.IsTrue(_runtime.IsGuideActive(1));
        }

        [Test]
        public void HideGuide_DeactivatesGuide()
        {
            _runtime.Init();
            _runtime.ShowGuide(1);
            _runtime.HideGuide(1);
            Assert.IsFalse(_runtime.IsGuideActive(1));
        }

        [Test]
        public void HideAll_ClearsAllGuides()
        {
            _runtime.Init();
            _runtime.ShowGuide(1);
            _runtime.ShowGuide(2);
            _runtime.HideAll();
            Assert.IsFalse(_runtime.IsGuideActive(1));
            Assert.IsFalse(_runtime.IsGuideActive(2));
        }

        [Test]
        public void ShowGuide_FiresEvent()
        {
            _runtime.Init();
            _guideShownFireCount = 0;
            _runtime.OnGuideShown += HandleGuideShown;
            _runtime.ShowGuide(1);
            Assert.AreEqual(1, _guideShownFireCount);
            _runtime.OnGuideShown -= HandleGuideShown;
        }

        [Test]
        public void HideGuide_FiresEvent()
        {
            _runtime.Init();
            _runtime.ShowGuide(1);
            _guideHiddenFireCount = 0;
            _runtime.OnGuideHidden += HandleGuideHidden;
            _runtime.HideGuide(1);
            Assert.AreEqual(1, _guideHiddenFireCount);
            _runtime.OnGuideHidden -= HandleGuideHidden;
        }

        int _guideShownFireCount;
        int _guideHiddenFireCount;

        void HandleGuideShown(int guideId)
        {
            _guideShownFireCount++;
        }

        void HandleGuideHidden(int guideId)
        {
            _guideHiddenFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
