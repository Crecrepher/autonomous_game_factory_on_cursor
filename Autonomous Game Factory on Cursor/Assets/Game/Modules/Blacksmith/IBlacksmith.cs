namespace Game
{
    public interface IBlacksmith
    {
        void Init();
        void Tick(float deltaTime);

        int ForgeCount { get; }
        int MaxForgeCount { get; }
        int ForgeCost { get; }
        int EquipmentPerForge { get; }

        bool TryForge();

        event System.Action<int> OnForged;
    }
}
