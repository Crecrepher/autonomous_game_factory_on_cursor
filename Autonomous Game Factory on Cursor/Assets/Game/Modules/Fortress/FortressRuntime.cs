using System;

namespace Game
{
    public class FortressRuntime : IFortress
    {
        const int MIN_HP = 0;
        const int BASE_LEVEL = 0;
        const int UPGRADE_LEVEL_1 = 1;
        const int UPGRADE_LEVEL_2 = 2;
        const int INVALID_COST = 0;

        public event Action<int> OnHpChanged;
        public event Action<int> OnUpgraded;

        public int CurrentHp => _currentHp;
        public int MaxHp => _config.MaxHp;
        public int UpgradeLevel => _upgradeLevel;

        readonly FortressConfig _config;
        int _currentHp;
        int _upgradeLevel;

        public FortressRuntime(FortressConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _currentHp = _config.MaxHp;
            _upgradeLevel = BASE_LEVEL;
        }

        public void Tick(float deltaTime)
        {
        }

        public int GetUpgradeCost(int level)
        {
            if (level == UPGRADE_LEVEL_1)
                return _config.Upgrade1Cost;
            if (level == UPGRADE_LEVEL_2)
                return _config.Upgrade2Cost;
            return INVALID_COST;
        }

        public bool TryUpgrade()
        {
            if (_upgradeLevel >= _config.MaxUpgradeLevel)
                return false;

            _upgradeLevel++;

            if (OnUpgraded != null)
                OnUpgraded.Invoke(_upgradeLevel);

            return true;
        }

        public void TakeDamage(int damage)
        {
            _currentHp -= damage;
            if (_currentHp < MIN_HP)
                _currentHp = MIN_HP;

            if (OnHpChanged != null)
                OnHpChanged.Invoke(_currentHp);
        }
    }
}
