using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "HireNodesConfig", menuName = "Game/Modules/HireNodesConfig")]
    public class HireNodesConfig : ScriptableObject
    {
        [SerializeField] int _maxNodes = 10;
        [SerializeField] int _defaultBatchHireCount = 1;

        public int MaxNodes => _maxNodes;
        public int DefaultBatchHireCount => _defaultBatchHireCount;
    }
}
