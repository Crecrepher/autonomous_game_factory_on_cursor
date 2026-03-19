using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002InputGuideTests
    {
        [Test]
        public void Create_WithConfig_ReturnsNonNull()
        {
            P002InputGuideConfig config = ScriptableObject.CreateInstance<P002InputGuideConfig>();
            IP002InputGuide runtime = P002InputGuideFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Create_WithNullConfig_ReturnsNull()
        {
            IP002InputGuide runtime = P002InputGuideFactory.Create(null);
            Assert.IsNull(runtime);
        }

        [Test]
        public void ShowGuide_SetsVisible()
        {
            P002InputGuideConfig config = ScriptableObject.CreateInstance<P002InputGuideConfig>();
            IP002InputGuide runtime = P002InputGuideFactory.Create(config);
            runtime.Init(6, 8);
            Assert.IsFalse(runtime.IsGuideVisible);
            runtime.ShowGuide(4, 2, 3, 2);
            Assert.IsTrue(runtime.IsGuideVisible);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Init_ThenTick_DoesNotThrow()
        {
            P002InputGuideConfig config = ScriptableObject.CreateInstance<P002InputGuideConfig>();
            IP002InputGuide runtime = P002InputGuideFactory.Create(config);
            runtime.Init(6, 8);
            Assert.DoesNotThrow(() => runtime.Tick(0.016f));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void NotifyInputDetected_ResetsIdleAndHidesGuide()
        {
            P002InputGuideConfig config = ScriptableObject.CreateInstance<P002InputGuideConfig>();
            IP002InputGuide runtime = P002InputGuideFactory.Create(config);
            runtime.Init(6, 8);
            runtime.ShowGuide(4, 2, 3, 2);
            runtime.NotifyInputDetected();
            Assert.IsFalse(runtime.IsGuideVisible);
            Object.DestroyImmediate(config);
        }
    }
}
