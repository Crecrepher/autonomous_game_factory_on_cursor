using System;

namespace Game
{
    public class BlacksmithRuntime : IBlacksmith
    {
        const int MIN_FORGE_COUNT = 0;

        public event Action<int> OnForged;

        public int ForgeCount => _forgeCount;
        public int MaxForgeCount => _config.MaxForgeCount;
        public int ForgeCost => _config.ForgeCost;
        public int EquipmentPerForge => _config.EquipmentPerForge;

        readonly BlacksmithConfig _config;
        int _forgeCount;

        public BlacksmithRuntime(BlacksmithConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _forgeCount = MIN_FORGE_COUNT;
        }

        public void Tick(float deltaTime)
        {
        }

        public bool TryForge()
        {
            if (_forgeCount >= _config.MaxForgeCount)
                return false;

            _forgeCount++;

            if (OnForged != null)
                OnForged.Invoke(_forgeCount);

            return true;
        }
    }
}
