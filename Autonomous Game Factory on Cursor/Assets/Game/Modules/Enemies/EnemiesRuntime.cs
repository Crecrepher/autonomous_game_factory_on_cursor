using System;

namespace Game
{
    public class EnemiesRuntime : IEnemies
    {
        const int MIN_ALIVE = 0;
        const float MIN_SPAWN_RATE = 0.01f;

        public event Action<int> OnEnemyKilled;
        public event Action<int> OnWaveStarted;

        public int AliveCount => _aliveCount;
        public int TotalSpawned => _totalSpawned;
        public int TotalKilled => _totalKilled;

        readonly EnemiesConfig _config;
        float _spawnInterval;
        float _inverseSpawnInterval;
        float _spawnTimer;
        int _aliveCount;
        int _totalSpawned;
        int _totalKilled;
        int _waveIndex;
        bool _isSpawning;

        public EnemiesRuntime(EnemiesConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _spawnInterval = _config.SpawnInterval;
            _inverseSpawnInterval = 1f / _spawnInterval;
            _spawnTimer = 0f;
            _aliveCount = MIN_ALIVE;
            _totalSpawned = 0;
            _totalKilled = 0;
            _waveIndex = 0;
            _isSpawning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_isSpawning)
                return;

            _spawnTimer += deltaTime;

            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer -= _spawnInterval;
                TrySpawn();
            }
        }

        public void StartSpawning()
        {
            _isSpawning = true;
            _spawnTimer = 0f;
            _waveIndex++;

            if (OnWaveStarted != null)
                OnWaveStarted.Invoke(_waveIndex);
        }

        public void StopSpawning()
        {
            _isSpawning = false;
        }

        public void SetSpawnRate(float enemiesPerSecond)
        {
            if (enemiesPerSecond < MIN_SPAWN_RATE)
                enemiesPerSecond = MIN_SPAWN_RATE;

            _spawnInterval = 1f / enemiesPerSecond;
            _inverseSpawnInterval = enemiesPerSecond;
        }

        public void RegisterKill(int coinDrop)
        {
            _totalKilled++;
            if (_aliveCount > MIN_ALIVE)
                _aliveCount--;

            if (OnEnemyKilled != null)
                OnEnemyKilled.Invoke(coinDrop);
        }

        void TrySpawn()
        {
            if (_aliveCount >= _config.MaxAliveEnemies)
                return;

            _aliveCount++;
            _totalSpawned++;
        }
    }
}
