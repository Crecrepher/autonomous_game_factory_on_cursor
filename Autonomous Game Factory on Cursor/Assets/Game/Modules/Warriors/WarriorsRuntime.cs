using System;

namespace Game
{
    public class WarriorsRuntime : IWarriors
    {
        const int MIN_HIRED = 0;
        const int MIN_COST = 1;

        public event Action<int> OnWarriorHired;
        public event Action<int> OnHireCostChanged;

        public int HiredCount => _hiredCount;
        public int MaxWarriors => _config.MaxWarriors;
        public int HireCost => _hireCost;

        readonly WarriorsConfig _config;
        int _hiredCount;
        int _hireCost;
        int _hireCostBase;
        int _hireCostIncrement;

        public WarriorsRuntime(WarriorsConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _hiredCount = MIN_HIRED;
            _hireCostBase = _config.HireCostBase;
            _hireCostIncrement = _config.HireCostIncrement;
            _hireCost = _hireCostBase;
        }

        public void Tick(float deltaTime)
        {
        }

        public bool TryHire()
        {
            if (_hiredCount >= _config.MaxWarriors)
                return false;

            _hiredCount++;

            if (OnWarriorHired != null)
                OnWarriorHired.Invoke(_hiredCount);

            UpdateHireCost();
            return true;
        }

        public void SetHireCostBase(int baseCost)
        {
            if (baseCost < MIN_COST)
                baseCost = MIN_COST;

            _hireCostBase = baseCost;
            UpdateHireCost();
        }

        public void SetHireCostIncrement(int increment)
        {
            _hireCostIncrement = increment;
            UpdateHireCost();
        }

        void UpdateHireCost()
        {
            _hireCost = _hireCostBase + (_hiredCount * _hireCostIncrement);
            if (_hireCost > _config.HireCostMax)
                _hireCost = _config.HireCostMax;

            if (OnHireCostChanged != null)
                OnHireCostChanged.Invoke(_hireCost);
        }
    }
}
