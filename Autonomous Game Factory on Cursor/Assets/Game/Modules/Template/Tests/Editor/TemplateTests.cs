using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class TemplateTests
    {
        [Test]
        public void CreateRuntime_WithConfig_ReturnsNonNull()
        {
            var config = ScriptableObject.CreateInstance<TemplateConfig>();
            ITemplate runtime = TemplateFactory.CreateRuntime(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Init_ThenTick_DoesNotThrow()
        {
            var config = ScriptableObject.CreateInstance<TemplateConfig>();
            ITemplate runtime = TemplateFactory.CreateRuntime(config);
            runtime.Init();
            Assert.DoesNotThrow(() => runtime.Tick(0.016f));
            Object.DestroyImmediate(config);
        }
    }
}
