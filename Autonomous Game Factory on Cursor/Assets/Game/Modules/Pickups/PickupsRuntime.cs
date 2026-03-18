using System;

namespace Game
{
    public class PickupsRuntime : IPickups
    {
        const int EMPTY_SLOT = 0;

        public event Action<int> OnPickupCollected;

        public int PendingCount => _pendingCount;
        public int TotalCollected => _totalCollected;

        readonly PickupsConfig _config;
        int[] _pendingValues;
        int _pendingCount;
        int _totalCollected;

        public PickupsRuntime(PickupsConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _pendingValues = new int[_config.MaxPendingPickups];
            _pendingCount = 0;
            _totalCollected = 0;

            for (int i = 0; i < _pendingValues.Length; i++)
            {
                _pendingValues[i] = EMPTY_SLOT;
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void SpawnPickup(int coinValue)
        {
            if (_pendingCount >= _config.MaxPendingPickups)
                return;

            for (int i = 0; i < _pendingValues.Length; i++)
            {
                if (_pendingValues[i] == EMPTY_SLOT)
                {
                    _pendingValues[i] = coinValue;
                    _pendingCount++;
                    return;
                }
            }
        }

        public void CollectAll()
        {
            for (int i = 0; i < _pendingValues.Length; i++)
            {
                if (_pendingValues[i] != EMPTY_SLOT)
                {
                    int value = _pendingValues[i];
                    _pendingValues[i] = EMPTY_SLOT;
                    _pendingCount--;
                    _totalCollected++;

                    if (OnPickupCollected != null)
                        OnPickupCollected.Invoke(value);
                }
            }
        }
    }
}
