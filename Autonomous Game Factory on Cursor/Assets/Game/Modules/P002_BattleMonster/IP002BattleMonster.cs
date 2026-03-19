using System;

namespace Game
{
    public interface IP002BattleMonster
    {
        void Init(IP002GameConfig config);
        void Tick(float deltaTime);
        void ApplyBasicAttack(int characterIndex);
        void ApplySkillAttack(int characterIndex);
        void ApplySkillDamageChunk(int characterIndex, int damage);

        int GetSkillDamage();
        int GetCurrentMonsterHealth();
        float GetCurrentMonsterHealthRatio();

        int CurrentMonsterIndex { get; }
        bool IsAllMonstersDead { get; }

        event Action<int> OnMonsterDefeated;
        event Action OnAllMonstersDefeated;
        event Action<int, int, int> OnMonsterHealthChanged;
    }
}
