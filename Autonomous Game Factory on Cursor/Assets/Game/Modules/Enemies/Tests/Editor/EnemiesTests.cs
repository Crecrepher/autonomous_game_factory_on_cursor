using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class EnemiesTests
    {
        EnemiesConfig _config;
        IEnemies _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EnemiesConfig>();
            _runtime = EnemiesFactory.CreateRuntime(_config);
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
        public void Init_SetsDefaultValues()
        {
            _runtime.Init();
            Assert.AreEqual(0, _runtime.AliveCount);
            Assert.AreEqual(0, _runtime.TotalSpawned);
            Assert.AreEqual(0, _runtime.TotalKilled);
        }

        [Test]
        public void StartSpawning_FiresWaveEvent()
        {
            _runtime.Init();
            _waveStartedFireCount = 0;
            _runtime.OnWaveStarted += HandleWaveStarted;
            _runtime.StartSpawning();
            Assert.AreEqual(1, _waveStartedFireCount);
            _runtime.OnWaveStarted -= HandleWaveStarted;
        }

        [Test]
        public void StopSpawning_PreventsSpawn()
        {
            _runtime.Init();
            _runtime.StartSpawning();
            _runtime.StopSpawning();
            int before = _runtime.TotalSpawned;
            _runtime.Tick(10f);
            Assert.AreEqual(before, _runtime.TotalSpawned);
        }

        [Test]
        public void RegisterKill_IncrementsKillCount()
        {
            _runtime.Init();
            _runtime.StartSpawning();
            _runtime.Tick(_config.SpawnInterval + 0.01f);
            EnemiesRuntime concrete = (EnemiesRuntime)_runtime;
            int beforeKilled = _runtime.TotalKilled;
            concrete.RegisterKill(_config.NormalEnemyCoinDrop);
            Assert.AreEqual(beforeKilled + 1, _runtime.TotalKilled);
        }

        [Test]
        public void OnEnemyKilled_FiresWithCoinDrop()
        {
            _runtime.Init();
            _runtime.StartSpawning();
            _runtime.Tick(_config.SpawnInterval + 0.01f);

            _lastCoinDrop = 0;
            _runtime.OnEnemyKilled += HandleEnemyKilled;
            EnemiesRuntime concrete = (EnemiesRuntime)_runtime;
            concrete.RegisterKill(_config.NormalEnemyCoinDrop);
            Assert.AreEqual(_config.NormalEnemyCoinDrop, _lastCoinDrop);
            _runtime.OnEnemyKilled -= HandleEnemyKilled;
        }

        int _waveStartedFireCount;
        int _lastCoinDrop;

        void HandleWaveStarted(int waveIndex)
        {
            _waveStartedFireCount++;
        }

        void HandleEnemyKilled(int coinDrop)
        {
            _lastCoinDrop = coinDrop;
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
