using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002GameConfigTests
    {
        [Test]
        public void CreateRuntime_WithConfig_ReturnsNonNull()
        {
            P002GameConfigConfig config = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig runtime = P002GameConfigFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BoardWidth_Default_Returns6()
        {
            P002GameConfigConfig config = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig runtime = P002GameConfigFactory.Create(config);
            Assert.AreEqual(6, runtime.BoardWidth);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SkillGaugeMax_Default_Returns50()
        {
            P002GameConfigConfig config = ScriptableObject.CreateInstance<P002GameConfigConfig>();
            IP002GameConfig runtime = P002GameConfigFactory.Create(config);
            Assert.AreEqual(50, runtime.SkillGaugeMax);
            Object.DestroyImmediate(config);
        }
    }
}
