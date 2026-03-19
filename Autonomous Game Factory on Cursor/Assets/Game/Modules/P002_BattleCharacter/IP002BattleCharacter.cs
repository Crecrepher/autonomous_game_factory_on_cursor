using System;

namespace Game
{
    public interface IP002BattleCharacter
    {
        event Action<int, int> OnSkillGaugeChanged;
        event Action<int> OnSkillReady;

        void Init(IP002GameConfig config);
        void Tick(float deltaTime);
        int GetCharacterIndex(int slot);
        EP002WeaponType GetWeaponType(int slot);
        float GetSkillGaugeRatio(int slot);
        bool IsSkillReady(int slot);
        void AddGauge(int slot, int pieceCount);
        void ResetGauge(int slot);
    }
}
