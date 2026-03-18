using System;

namespace Game
{
    public class DynamicConfigRuntime : IDynamicConfig
    {
        const int INVALID_KEY = -1;

        public event Action<int> OnValueChanged;

        readonly DynamicConfigConfig _config;
        int[] _keys;
        float[] _floatValues;
        int[] _intValues;
        bool[] _isFloat;
        int _count;

        public DynamicConfigRuntime(DynamicConfigConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            int max = _config.MaxEntries;
            _keys = new int[max];
            _floatValues = new float[max];
            _intValues = new int[max];
            _isFloat = new bool[max];
            _count = 0;

            for (int i = 0; i < max; i++)
            {
                _keys[i] = INVALID_KEY;
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public float GetFloat(int key, float defaultValue)
        {
            int index = FindIndex(key);
            if (index < 0)
                return defaultValue;

            return _floatValues[index];
        }

        public int GetInt(int key, int defaultValue)
        {
            int index = FindIndex(key);
            if (index < 0)
                return defaultValue;

            return _intValues[index];
        }

        public void SetFloat(int key, float value)
        {
            int index = FindIndex(key);
            if (index < 0)
            {
                index = AllocateSlot(key);
                if (index < 0)
                    return;
            }

            _floatValues[index] = value;
            _isFloat[index] = true;

            if (OnValueChanged != null)
                OnValueChanged.Invoke(key);
        }

        public void SetInt(int key, int value)
        {
            int index = FindIndex(key);
            if (index < 0)
            {
                index = AllocateSlot(key);
                if (index < 0)
                    return;
            }

            _intValues[index] = value;
            _isFloat[index] = false;

            if (OnValueChanged != null)
                OnValueChanged.Invoke(key);
        }

        public bool HasKey(int key)
        {
            return FindIndex(key) >= 0;
        }

        int FindIndex(int key)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_keys[i] == key)
                    return i;
            }
            return -1;
        }

        int AllocateSlot(int key)
        {
            if (_count >= _config.MaxEntries)
                return -1;

            int index = _count;
            _keys[index] = key;
            _count++;
            return index;
        }
    }
}
