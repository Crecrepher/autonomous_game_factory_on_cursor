using System;

namespace Game
{
    public class BuffIconUIRuntime : IBuffIconUI
    {
        const int INVALID_EFFECT_ID = -1;
        const float EXPIRED_DURATION = 0f;

        public event Action<int> OnIconAdded;
        public event Action<int> OnIconRemoved;

        public int ActiveIconCount => _activeCount;

        readonly BuffIconUIConfig _config;
        int[] _effectIds;
        float[] _durations;
        int _activeCount;

        public BuffIconUIRuntime(BuffIconUIConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            int max = _config.MaxIcons;
            _effectIds = new int[max];
            _durations = new float[max];
            _activeCount = 0;

            for (int i = 0; i < max; i++)
            {
                _effectIds[i] = INVALID_EFFECT_ID;
                _durations[i] = EXPIRED_DURATION;
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _effectIds.Length; i++)
            {
                if (_effectIds[i] == INVALID_EFFECT_ID)
                    continue;

                _durations[i] -= deltaTime;

                if (_durations[i] <= EXPIRED_DURATION)
                {
                    int expiredId = _effectIds[i];
                    _effectIds[i] = INVALID_EFFECT_ID;
                    _durations[i] = EXPIRED_DURATION;
                    _activeCount--;

                    if (OnIconRemoved != null)
                        OnIconRemoved.Invoke(expiredId);
                }
            }
        }

        public void AddIcon(int effectId, float duration)
        {
            int existingIndex = FindIndex(effectId);
            if (existingIndex >= 0)
            {
                _durations[existingIndex] = duration;
                return;
            }

            if (_activeCount >= _config.MaxIcons)
                return;

            for (int i = 0; i < _effectIds.Length; i++)
            {
                if (_effectIds[i] == INVALID_EFFECT_ID)
                {
                    _effectIds[i] = effectId;
                    _durations[i] = duration;
                    _activeCount++;

                    if (OnIconAdded != null)
                        OnIconAdded.Invoke(effectId);

                    return;
                }
            }
        }

        public void RemoveIcon(int effectId)
        {
            int index = FindIndex(effectId);
            if (index < 0)
                return;

            _effectIds[index] = INVALID_EFFECT_ID;
            _durations[index] = EXPIRED_DURATION;
            _activeCount--;

            if (OnIconRemoved != null)
                OnIconRemoved.Invoke(effectId);
        }

        public void ClearAll()
        {
            for (int i = 0; i < _effectIds.Length; i++)
            {
                if (_effectIds[i] != INVALID_EFFECT_ID)
                {
                    int id = _effectIds[i];
                    _effectIds[i] = INVALID_EFFECT_ID;
                    _durations[i] = EXPIRED_DURATION;

                    if (OnIconRemoved != null)
                        OnIconRemoved.Invoke(id);
                }
            }
            _activeCount = 0;
        }

        int FindIndex(int effectId)
        {
            for (int i = 0; i < _effectIds.Length; i++)
            {
                if (_effectIds[i] == effectId)
                    return i;
            }
            return -1;
        }
    }
}
