using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "FortressConfig", menuName = "Game/Modules/FortressConfig")]
    public class FortressConfig : ScriptableObject
    {
        [SerializeField] int _maxHp = 100;
        [SerializeField] int _maxUpgradeLevel = 2;
        [SerializeField] int _upgrade1Cost = 10;
        [SerializeField] int _upgrade2Cost = 80;

        public int MaxHp => _maxHp;
        public int MaxUpgradeLevel => _maxUpgradeLevel;
        public int Upgrade1Cost => _upgrade1Cost;
        public int Upgrade2Cost => _upgrade2Cost;
    }
}
