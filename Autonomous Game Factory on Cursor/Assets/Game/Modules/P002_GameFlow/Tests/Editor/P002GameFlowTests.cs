using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002GameFlowTests
    {
        [Test]
        public void Create_ReturnsNonNullRuntime()
        {
            var config = ScriptableObject.CreateInstance<P002GameFlowConfig>();
            var runtime = P002GameFlowFactory.Create(config);
            Assert.IsNotNull(runtime);
        }

        [Test]
        public void StartGame_SetsStateToPlaying()
        {
            var config = ScriptableObject.CreateInstance<P002GameFlowConfig>();
            var runtime = P002GameFlowFactory.Create(config) as P002GameFlowRuntime;
            Assert.IsNotNull(runtime);

            runtime.StartGame();

            Assert.IsTrue(runtime.IsGameStarted);
            Assert.AreEqual(EP002GameState.Playing, runtime.CurrentState);
        }

        [Test]
        public void EndGame_SetsStateToEnded()
        {
            var config = ScriptableObject.CreateInstance<P002GameFlowConfig>();
            var runtime = P002GameFlowFactory.Create(config) as P002GameFlowRuntime;
            Assert.IsNotNull(runtime);

            runtime.StartGame();
            runtime.EndGame(true);

            Assert.IsTrue(runtime.IsGameEnded);
            Assert.AreEqual(EP002GameState.Ended, runtime.CurrentState);
        }

        [Test]
        public void Init_SetsStateToInitializing()
        {
            var config = ScriptableObject.CreateInstance<P002GameFlowConfig>();
            var runtime = P002GameFlowFactory.Create(config) as P002GameFlowRuntime;
            Assert.IsNotNull(runtime);

            Assert.AreEqual(EP002GameState.Initializing, runtime.CurrentState);
            Assert.AreEqual(0, runtime.CurrentStage);
        }
    }
}
