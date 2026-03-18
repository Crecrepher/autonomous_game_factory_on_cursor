namespace Game
{
    public interface IWarriors
    {
        void Init();
        void Tick(float deltaTime);

        int HiredCount { get; }
        int MaxWarriors { get; }
        int HireCost { get; }

        bool TryHire();
        void SetHireCostBase(int baseCost);
        void SetHireCostIncrement(int increment);

        event System.Action<int> OnWarriorHired;
        event System.Action<int> OnHireCostChanged;
    }
}
