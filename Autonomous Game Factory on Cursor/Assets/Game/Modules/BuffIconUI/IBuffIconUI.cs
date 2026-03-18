namespace Game
{
    public interface IBuffIconUI
    {
        void Init();
        void Tick(float deltaTime);

        int ActiveIconCount { get; }
        void AddIcon(int effectId, float duration);
        void RemoveIcon(int effectId);
        void ClearAll();

        event System.Action<int> OnIconAdded;
        event System.Action<int> OnIconRemoved;
    }
}
