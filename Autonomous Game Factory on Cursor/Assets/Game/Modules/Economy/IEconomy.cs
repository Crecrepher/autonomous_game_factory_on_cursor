namespace Game
{
    public interface IEconomy
    {
        void Init();
        void Tick(float deltaTime);

        int Balance { get; }
        bool CanAfford(int amount);
        bool TrySpend(int amount);
        void Add(int amount);

        event System.Action<int> OnBalanceChanged;
        event System.Action<int> OnCoinAdded;
        event System.Action<int> OnCoinSpent;
    }
}
