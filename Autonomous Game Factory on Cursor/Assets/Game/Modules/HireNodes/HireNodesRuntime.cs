using System;

namespace Game
{
    public class HireNodesRuntime : IHireNodes
    {
        const int INACTIVE = 0;
        const int ACTIVE = 1;
        const int MIN_BATCH = 1;

        public event Action<int> OnNodeActivated;
        public event Action<int> OnNodeDeactivated;

        public int NodeCount => _config.MaxNodes;
        public int ActiveNodeCount => _activeCount;

        readonly HireNodesConfig _config;
        int[] _nodeStates;
        int _activeCount;
        int _batchHireCount;

        public HireNodesRuntime(HireNodesConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _nodeStates = new int[_config.MaxNodes];
            _activeCount = 0;
            _batchHireCount = _config.DefaultBatchHireCount;

            for (int i = 0; i < _nodeStates.Length; i++)
            {
                _nodeStates[i] = INACTIVE;
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void ActivateNode(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= _config.MaxNodes)
                return;

            if (_nodeStates[nodeIndex] == ACTIVE)
                return;

            _nodeStates[nodeIndex] = ACTIVE;
            _activeCount++;

            if (OnNodeActivated != null)
                OnNodeActivated.Invoke(nodeIndex);
        }

        public void DeactivateNode(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= _config.MaxNodes)
                return;

            if (_nodeStates[nodeIndex] == INACTIVE)
                return;

            _nodeStates[nodeIndex] = INACTIVE;
            _activeCount--;

            if (OnNodeDeactivated != null)
                OnNodeDeactivated.Invoke(nodeIndex);
        }

        public bool IsNodeActive(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= _config.MaxNodes)
                return false;

            return _nodeStates[nodeIndex] == ACTIVE;
        }

        public void SetBatchHireCount(int count)
        {
            if (count < MIN_BATCH)
                count = MIN_BATCH;

            _batchHireCount = count;
        }

        public int GetBatchHireCount()
        {
            return _batchHireCount;
        }
    }
}
