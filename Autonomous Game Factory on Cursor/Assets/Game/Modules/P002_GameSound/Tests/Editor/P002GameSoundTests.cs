using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002GameSoundTests
    {
        [Test]
        public void Create_WithConfig_ReturnsNonNull()
        {
            P002GameSoundConfig config = ScriptableObject.CreateInstance<P002GameSoundConfig>();
            IP002GameSound runtime = P002GameSoundFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PlayBlockDrop_SetsCorrectLastPlayedSoundIndex()
        {
            P002GameSoundConfig config = ScriptableObject.CreateInstance<P002GameSoundConfig>();
            IP002GameSound runtime = P002GameSoundFactory.Create(config);
            Assert.IsNotNull(runtime);
            runtime.Init();
            P002GameSoundRuntime concreteRuntime = runtime as P002GameSoundRuntime;
            Assert.IsNotNull(concreteRuntime);
            concreteRuntime.PlayBlockDrop();
            Assert.AreEqual(P002GameSoundConfig.SOUND_BLOCK_DROP, runtime.LastPlayedSoundIndex);
            Object.DestroyImmediate(config);
        }
    }
}
