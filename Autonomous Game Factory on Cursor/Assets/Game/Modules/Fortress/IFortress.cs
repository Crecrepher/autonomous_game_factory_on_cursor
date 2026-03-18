namespace Game
{
    public interface IFortress
    {
        void Init();
        void Tick(float deltaTime);

        int CurrentHp { get; }
        int MaxHp { get; }
        int UpgradeLevel { get; }
        int GetUpgradeCost(int level);

        bool TryUpgrade();
        void TakeDamage(int damage);

        event System.Action<int> OnHpChanged;
        event System.Action<int> OnUpgraded;
    }
}
