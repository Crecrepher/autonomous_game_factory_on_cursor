using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Game/Modules/EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [SerializeField] int _startingBalance = 6;
        [SerializeField] int _maxBalance = 99999;

        public int StartingBalance => _startingBalance;
        public int MaxBalance => _maxBalance;
    }
}
