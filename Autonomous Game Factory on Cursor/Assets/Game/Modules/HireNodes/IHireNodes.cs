namespace Game
{
    public interface IHireNodes
    {
        void Init();
        void Tick(float deltaTime);

        int NodeCount { get; }
        int ActiveNodeCount { get; }

        void ActivateNode(int nodeIndex);
        void DeactivateNode(int nodeIndex);
        bool IsNodeActive(int nodeIndex);
        void SetBatchHireCount(int count);

        event System.Action<int> OnNodeActivated;
        event System.Action<int> OnNodeDeactivated;
    }
}
