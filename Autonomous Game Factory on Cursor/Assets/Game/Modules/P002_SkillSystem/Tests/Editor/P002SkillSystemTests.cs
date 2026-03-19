using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002SkillSystemTests
    {
        [Test]
        public void Create_WithConfig_ReturnsNonNull()
        {
            var config = ScriptableObject.CreateInstance<P002SkillSystemConfig>();
            IP002SkillSystem runtime = P002SkillSystemFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AddGauge_FillsGauge()
        {
            var config = ScriptableObject.CreateInstance<P002SkillSystemConfig>();
            IP002SkillSystem runtime = P002SkillSystemFactory.Create(config);
            runtime.Init(null);

            runtime.AddGauge(0, 5);
            Assert.GreaterOrEqual(runtime.GetGaugeRatio(0), 0.99f, "5 pieces * 10 gauge should fill 50 max gauge");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void TryActivateSkill_WhenReady_ReturnsTrue()
        {
            var config = ScriptableObject.CreateInstance<P002SkillSystemConfig>();
            IP002SkillSystem runtime = P002SkillSystemFactory.Create(config);
            runtime.Init(null);
            runtime.AddGauge(0, 5);

            bool result = runtime.TryActivateSkill(0);
            Assert.IsTrue(result, "Skill should activate when gauge is full");
            Object.DestroyImmediate(config);
        }
    }
}
