namespace Game
{
    public interface IDefenseTowers
    {
        void Init();
        void Tick(float deltaTime);

        int BuiltCount { get; }
        int MaxTowers { get; }
        int GetBuildCost(int towerIndex);

        bool TryBuild(int towerIndex);
        bool IsTowerBuilt(int towerIndex);

        event System.Action<int> OnTowerBuilt;
    }
}
