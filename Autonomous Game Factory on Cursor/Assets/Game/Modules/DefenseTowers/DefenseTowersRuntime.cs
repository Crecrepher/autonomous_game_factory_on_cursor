using System;

namespace Game
{
    public class DefenseTowersRuntime : IDefenseTowers
    {
        const int NOT_BUILT = 0;
        const int BUILT = 1;

        public event Action<int> OnTowerBuilt;

        public int BuiltCount => _builtCount;
        public int MaxTowers => _config.MaxTowers;

        readonly DefenseTowersConfig _config;
        int[] _towerStates;
        int _builtCount;

        public DefenseTowersRuntime(DefenseTowersConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _towerStates = new int[_config.MaxTowers];
            _builtCount = 0;

            for (int i = 0; i < _towerStates.Length; i++)
            {
                _towerStates[i] = NOT_BUILT;
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public int GetBuildCost(int towerIndex)
        {
            if (towerIndex < 0 || towerIndex >= _config.MaxTowers)
                return 0;

            if (towerIndex < _config.EarlyTowerCount)
                return _config.EarlyCost;

            return _config.LateCost;
        }

        public bool TryBuild(int towerIndex)
        {
            if (towerIndex < 0 || towerIndex >= _config.MaxTowers)
                return false;

            if (_towerStates[towerIndex] == BUILT)
                return false;

            _towerStates[towerIndex] = BUILT;
            _builtCount++;

            if (OnTowerBuilt != null)
                OnTowerBuilt.Invoke(towerIndex);

            return true;
        }

        public bool IsTowerBuilt(int towerIndex)
        {
            if (towerIndex < 0 || towerIndex >= _config.MaxTowers)
                return false;

            return _towerStates[towerIndex] == BUILT;
        }
    }
}
