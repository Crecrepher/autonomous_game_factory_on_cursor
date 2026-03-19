using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002PuzzleViewTests
    {
        [Test]
        public void Create_WithConfig_ReturnsNonNull()
        {
            P002PuzzleViewConfig config = ScriptableObject.CreateInstance<P002PuzzleViewConfig>();
            IP002PuzzleView runtime = P002PuzzleViewFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Create_WithNullConfig_ReturnsNull()
        {
            IP002PuzzleView runtime = P002PuzzleViewFactory.Create(null);
            Assert.IsNull(runtime);
        }

        [Test]
        public void NotifyBlockChanged_UpdatesDisplay()
        {
            P002PuzzleViewConfig config = ScriptableObject.CreateInstance<P002PuzzleViewConfig>();
            IP002PuzzleView runtime = P002PuzzleViewFactory.Create(config);
            runtime.Init(6, 8);
            runtime.NotifyBlockChanged(2, 3, 1, 0);
            runtime.NotifyBlockChanged(2, 3, 2, 1);
            Assert.AreEqual(2, runtime.GetDisplayedBlockType(2, 3));
            Assert.AreEqual(1, runtime.GetDisplayedSpecialType(2, 3));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Init_ThenTick_DoesNotThrow()
        {
            P002PuzzleViewConfig config = ScriptableObject.CreateInstance<P002PuzzleViewConfig>();
            IP002PuzzleView runtime = P002PuzzleViewFactory.Create(config);
            runtime.Init(6, 8);
            Assert.DoesNotThrow(() => runtime.Tick(0.016f));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void NotifySwapStarted_IncrementsAnimationCount()
        {
            P002PuzzleViewConfig config = ScriptableObject.CreateInstance<P002PuzzleViewConfig>();
            IP002PuzzleView runtime = P002PuzzleViewFactory.Create(config);
            runtime.Init(6, 8);
            runtime.NotifyBlockChanged(0, 0, 1, 0);
            runtime.NotifyBlockChanged(1, 0, 2, 0);
            Assert.IsFalse(runtime.IsAnimating);
            runtime.NotifySwapStarted(0, 0, 1, 0);
            Assert.IsTrue(runtime.IsAnimating);
            runtime.NotifySwapCompleted(true);
            Assert.IsFalse(runtime.IsAnimating);
            Object.DestroyImmediate(config);
        }
    }
}
