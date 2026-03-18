namespace Game
{
    public interface IPickups
    {
        void Init();
        void Tick(float deltaTime);

        int PendingCount { get; }
        int TotalCollected { get; }

        void SpawnPickup(int coinValue);
        void CollectAll();

        event System.Action<int> OnPickupCollected;
    }
}
