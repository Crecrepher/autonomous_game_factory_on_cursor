using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002EndCardTests
    {
        [Test]
        public void Create_WithConfig_ReturnsNonNull()
        {
            P002EndCardConfig config = ScriptableObject.CreateInstance<P002EndCardConfig>();
            IP002EndCard runtime = P002EndCardFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            P002EndCardConfig config = ScriptableObject.CreateInstance<P002EndCardConfig>();
            IP002EndCard runtime = P002EndCardFactory.Create(config);
            runtime.Init();
            Assert.IsFalse(runtime.IsVisible);
            runtime.Show();
            Assert.IsTrue(runtime.IsVisible);
            runtime.Hide();
            Assert.IsFalse(runtime.IsVisible);
            Object.DestroyImmediate(config);
        }
    }
}
