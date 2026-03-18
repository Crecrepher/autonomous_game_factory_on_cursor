using System;

namespace Game
{
    public class EconomyRuntime : IEconomy
    {
        const int MIN_BALANCE = 0;

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnCoinAdded;
        public event Action<int> OnCoinSpent;

        public int Balance => _balance;

        readonly EconomyConfig _config;
        int _balance;

        public EconomyRuntime(EconomyConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _balance = _config.StartingBalance;
        }

        public void Tick(float deltaTime)
        {
        }

        public bool CanAfford(int amount)
        {
            return _balance >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (_balance < amount)
                return false;

            _balance -= amount;
            if (_balance < MIN_BALANCE)
                _balance = MIN_BALANCE;

            if (OnCoinSpent != null)
                OnCoinSpent.Invoke(amount);
            if (OnBalanceChanged != null)
                OnBalanceChanged.Invoke(_balance);
            return true;
        }

        public void Add(int amount)
        {
            _balance += amount;
            if (_balance > _config.MaxBalance)
                _balance = _config.MaxBalance;

            if (OnCoinAdded != null)
                OnCoinAdded.Invoke(amount);
            if (OnBalanceChanged != null)
                OnBalanceChanged.Invoke(_balance);
        }
    }
}
