using System;

namespace Game
{
    public class P002BattleCharacterRuntime : IP002BattleCharacter
    {
        public event Action<int, int> OnSkillGaugeChanged;
        public event Action<int> OnSkillReady;

        const int CHARACTER_COUNT = 3;

        int[] _skillGauges;
        int[] _skillGaugeMaxValues;
        float[] _maxInverseCache;
        bool[] _isSkillReady;
        EP002WeaponType[] _weaponTypes;
        float[] _gaugeRatioCache;
        P002BattleCharacterConfig _config;

        public P002BattleCharacterRuntime(P002BattleCharacterConfig config)
        {
            _config = config;
            _skillGauges = new int[CHARACTER_COUNT];
            _skillGaugeMaxValues = new int[CHARACTER_COUNT];
            _maxInverseCache = new float[CHARACTER_COUNT];
            _isSkillReady = new bool[CHARACTER_COUNT];
            _weaponTypes = new EP002WeaponType[CHARACTER_COUNT];
            _gaugeRatioCache = new float[CHARACTER_COUNT];
        }

        public void Init(IP002GameConfig config)
        {
            if (config == null || _config == null)
                return;

            for (int i = 0; i < CHARACTER_COUNT; i++)
            {
                _skillGaugeMaxValues[i] = config.SkillGaugeMax;
                _skillGauges[i] = config.GetInitialGauge(i);
                _weaponTypes[i] = _config.GetWeaponType(i);
                _isSkillReady[i] = false;
                UpdateGaugeRatioCache(i);
                FireGaugeChanged(i);
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public int GetCharacterIndex(int slot)
        {
            if (slot < 0 || slot >= CHARACTER_COUNT)
                return -1;
            return slot;
        }

        public EP002WeaponType GetWeaponType(int slot)
        {
            if (slot < 0 || slot >= CHARACTER_COUNT)
                return EP002WeaponType.None;
            return _weaponTypes[slot];
        }

        public float GetSkillGaugeRatio(int slot)
        {
            if (slot < 0 || slot >= CHARACTER_COUNT)
                return 0f;
            return _gaugeRatioCache[slot];
        }

        public bool IsSkillReady(int slot)
        {
            if (slot < 0 || slot >= CHARACTER_COUNT)
                return false;
            return _isSkillReady[slot];
        }

        public void AddGauge(int slot, int pieceCount)
        {
            if (slot < 0 || slot >= CHARACTER_COUNT || _config == null)
                return;

            int gaugePerPiece = _config.GaugePerPiece;
            int addAmount = pieceCount * gaugePerPiece;
            int prevGauge = _skillGauges[slot];
            int newGauge = prevGauge + addAmount;
            int maxVal = _skillGaugeMaxValues[slot];

            if (newGauge > maxVal)
                newGauge = maxVal;

            _skillGauges[slot] = newGauge;
            UpdateGaugeRatioCache(slot);
            FireGaugeChanged(slot);

            if (_skillGauges[slot] >= maxVal)
            {
                _isSkillReady[slot] = true;
                if (OnSkillReady != null) OnSkillReady.Invoke(slot);
            }
        }

        public void ResetGauge(int slot)
        {
            if (slot < 0 || slot >= CHARACTER_COUNT)
                return;

            _skillGauges[slot] = 0;
            _isSkillReady[slot] = false;
            UpdateGaugeRatioCache(slot);
            FireGaugeChanged(slot);
        }

        void UpdateGaugeRatioCache(int slot)
        {
            int maxVal = _skillGaugeMaxValues[slot];
            if (maxVal <= 0)
            {
                _maxInverseCache[slot] = 0f;
                _gaugeRatioCache[slot] = 0f;
                return;
            }
            _maxInverseCache[slot] = 1f / (float)maxVal;
            _gaugeRatioCache[slot] = (float)_skillGauges[slot] * _maxInverseCache[slot];
        }

        void FireGaugeChanged(int slot)
        {
            if (OnSkillGaugeChanged != null) OnSkillGaugeChanged.Invoke(slot, _skillGauges[slot]);
        }
    }
}
