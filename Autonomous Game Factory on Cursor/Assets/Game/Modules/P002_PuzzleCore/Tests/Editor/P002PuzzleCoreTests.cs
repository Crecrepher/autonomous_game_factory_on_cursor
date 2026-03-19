using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002PuzzleCoreTests
    {
        [Test]
        public void Create_WithConfig_ReturnsNonNull()
        {
            P002PuzzleCoreConfig config = ScriptableObject.CreateInstance<P002PuzzleCoreConfig>();
            IP002PuzzleCore runtime = P002PuzzleCoreFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Init_CreatesValidBoard_NoCellIsTypeZero()
        {
            P002PuzzleCoreConfig config = ScriptableObject.CreateInstance<P002PuzzleCoreConfig>();
            IP002PuzzleCore runtime = P002PuzzleCoreFactory.Create(config);
            runtime.Init(6, 5, 3, false, false);

            for (int x = 0; x < runtime.BoardWidth; x++)
            {
                for (int y = 0; y < runtime.BoardHeight; y++)
                {
                    int blockType = runtime.GetBlockType(x, y);
                    Assert.AreNotEqual(0, blockType, "Cell at (" + x + "," + y + ") should not be type 0");
                }
            }

            Object.DestroyImmediate(config);
        }

        [Test]
        public void TrySwap_WithInvalidPosition_ReturnsFalse()
        {
            P002PuzzleCoreConfig config = ScriptableObject.CreateInstance<P002PuzzleCoreConfig>();
            IP002PuzzleCore runtime = P002PuzzleCoreFactory.Create(config);
            runtime.Init(6, 5, 3, false, false);

            bool result = runtime.TrySwap(0, 0, 2, 2);

            Assert.IsFalse(result, "Swap with non-adjacent position should return false");
            Object.DestroyImmediate(config);
        }
    }
}
