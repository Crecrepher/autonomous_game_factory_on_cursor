using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "DynamicConfigConfig", menuName = "Game/Modules/DynamicConfigConfig")]
    public class DynamicConfigConfig : ScriptableObject
    {
        [SerializeField] int _maxEntries = 64;

        public int MaxEntries => _maxEntries;
    }
}
