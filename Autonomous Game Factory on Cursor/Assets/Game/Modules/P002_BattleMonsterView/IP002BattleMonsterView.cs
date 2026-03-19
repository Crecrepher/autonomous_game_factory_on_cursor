using System;

namespace Game
{
    public interface IP002BattleMonsterView
    {
        event Action<int> OnDeathAnimationComplete;
        event Action<int> OnSpawnAnimationComplete;
        void Init(int monsterCount);
        void UpdateHealth(int monsterIndex, int currentHealth, int maxHealth);
        void PlayHitEffect(int monsterIndex);
        void PlayDeathEffect(int monsterIndex);
        void PlaySpawnEffect(int monsterIndex);
        void ShowDamageText(int damage);
        void AddDamageToText(int damage);
        void SetActiveMonster(int monsterIndex);
    }
}
