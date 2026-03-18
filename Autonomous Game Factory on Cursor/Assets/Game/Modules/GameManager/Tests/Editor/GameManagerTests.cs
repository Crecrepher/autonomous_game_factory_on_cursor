using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class GameManagerTests
    {
        GameManagerConfig _config;
        IGameManager _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameManagerConfig>();
            _runtime = GameManagerFactory.CreateRuntime(_config);
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
        public void Init_SetsDefaults()
        {
            _runtime.Init();
            Assert.AreEqual(0, _runtime.CurrentPhase);
            Assert.IsFalse(_runtime.IsGameOver);
        }

        [Test]
        public void StartGame_FiresEvent()
        {
            _runtime.Init();
            _gameStartedFireCount = 0;
            _runtime.OnGameStarted += HandleGameStarted;
            _runtime.StartGame();
            Assert.AreEqual(1, _gameStartedFireCount);
            _runtime.OnGameStarted -= HandleGameStarted;
        }

        [Test]
        public void AdvancePhase_IncrementsPhase()
        {
            _runtime.Init();
            _runtime.StartGame();
            _runtime.AdvancePhase();
            Assert.AreEqual(1, _runtime.CurrentPhase);
        }

        [Test]
        public void EndGame_SetsGameOver()
        {
            _runtime.Init();
            _runtime.StartGame();
            _runtime.EndGame();
            Assert.IsTrue(_runtime.IsGameOver);
        }

        [Test]
        public void EndGame_FiresEvent()
        {
            _runtime.Init();
            _runtime.StartGame();
            _gameEndedFireCount = 0;
            _runtime.OnGameEnded += HandleGameEnded;
            _runtime.EndGame();
            Assert.AreEqual(1, _gameEndedFireCount);
            _runtime.OnGameEnded -= HandleGameEnded;
        }

        [Test]
        public void AdvancePhase_AfterGameOver_DoesNothing()
        {
            _runtime.Init();
            _runtime.StartGame();
            _runtime.EndGame();
            int phaseBefore = _runtime.CurrentPhase;
            _runtime.AdvancePhase();
            Assert.AreEqual(phaseBefore, _runtime.CurrentPhase);
        }

        int _gameStartedFireCount;
        int _gameEndedFireCount;

        void HandleGameStarted()
        {
            _gameStartedFireCount++;
        }

        void HandleGameEnded()
        {
            _gameEndedFireCount++;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
