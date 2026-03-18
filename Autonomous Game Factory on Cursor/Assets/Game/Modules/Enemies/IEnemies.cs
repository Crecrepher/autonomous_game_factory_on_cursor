namespace Game
{
    public interface IEnemies
    {
        void Init();
        void Tick(float deltaTime);

        int AliveCount { get; }
        int TotalSpawned { get; }
        int TotalKilled { get; }

        void StartSpawning();
        void StopSpawning();
        void SetSpawnRate(float enemiesPerSecond);

        event System.Action<int> OnEnemyKilled;
        event System.Action<int> OnWaveStarted;
    }
}
