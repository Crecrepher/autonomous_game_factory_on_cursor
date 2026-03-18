using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "PickupsConfig", menuName = "Game/Modules/PickupsConfig")]
    public class PickupsConfig : ScriptableObject
    {
        [SerializeField] int _maxPendingPickups = 50;
        [SerializeField] float _autoCollectDelay = 0.5f;

        public int MaxPendingPickups => _maxPendingPickups;
        public float AutoCollectDelay => _autoCollectDelay;
    }
}
