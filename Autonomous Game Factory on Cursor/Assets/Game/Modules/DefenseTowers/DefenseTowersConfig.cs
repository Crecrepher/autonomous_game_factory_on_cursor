using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "DefenseTowersConfig", menuName = "Game/Modules/DefenseTowersConfig")]
    public class DefenseTowersConfig : ScriptableObject
    {
        const int TOWER_COUNT = 6;

        [SerializeField] int _maxTowers = 6;
        [SerializeField] int _earlyCost = 5;
        [SerializeField] int _lateCost = 30;
        [SerializeField] int _earlyTowerCount = 2;
        [SerializeField] int _warriorsPerTower = 4;

        public int MaxTowers => _maxTowers;
        public int EarlyCost => _earlyCost;
        public int LateCost => _lateCost;
        public int EarlyTowerCount => _earlyTowerCount;
        public int WarriorsPerTower => _warriorsPerTower;
    }
}
