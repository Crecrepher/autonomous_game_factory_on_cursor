using System;

namespace Game
{
    public class P002BattleMonsterViewRuntime : IP002BattleMonsterView
    {
        public event Action<int> OnDeathAnimationComplete;
        public event Action<int> OnSpawnAnimationComplete;

        int _monsterCount;
        int _activeMonsterIndex;
        float[] _healthRatios;
        int _accumulatedDamage;

        public void Init(int monsterCount)
        {
            _monsterCount = monsterCount;
            _activeMonsterIndex = 0;
            _healthRatios = new float[monsterCount];
            _accumulatedDamage = 0;

            for (int i = 0; i < monsterCount; i++)
            {
                _healthRatios[i] = 1f;
            }
        }

        public void UpdateHealth(int monsterIndex, int currentHealth, int maxHealth)
        {
            if (monsterIndex < 0 || monsterIndex >= _monsterCount)
                return;

            float ratio = 0f;
            if (maxHealth > 0)
            {
                ratio = (float)currentHealth / (float)maxHealth;
                if (ratio < 0f) ratio = 0f;
                if (ratio > 1f) ratio = 1f;
            }

            _healthRatios[monsterIndex] = ratio;
        }

        public void PlayHitEffect(int monsterIndex)
        {
            if (monsterIndex < 0 || monsterIndex >= _monsterCount)
                return;
        }

        public void PlayDeathEffect(int monsterIndex)
        {
            if (monsterIndex < 0 || monsterIndex >= _monsterCount)
                return;
        }

        public void PlaySpawnEffect(int monsterIndex)
        {
            if (monsterIndex < 0 || monsterIndex >= _monsterCount)
                return;
        }

        public void ShowDamageText(int damage)
        {
            _accumulatedDamage = damage;
        }

        public void AddDamageToText(int damage)
        {
            _accumulatedDamage += damage;
        }

        public void SetActiveMonster(int monsterIndex)
        {
            if (monsterIndex < 0 || monsterIndex >= _monsterCount)
                return;

            _activeMonsterIndex = monsterIndex;
        }

        public void NotifyDeathAnimationComplete(int monsterIndex)
        {
            if (OnDeathAnimationComplete != null)
                OnDeathAnimationComplete.Invoke(monsterIndex);
        }

        public void NotifySpawnAnimationComplete(int monsterIndex)
        {
            if (OnSpawnAnimationComplete != null)
                OnSpawnAnimationComplete.Invoke(monsterIndex);
        }

        public int MonsterCount => _monsterCount;
        public int ActiveMonsterIndex => _activeMonsterIndex;
        public int AccumulatedDamage => _accumulatedDamage;

        public float GetHealthRatio(int index)
        {
            if (index < 0 || index >= _monsterCount)
                return 0f;
            return _healthRatios[index];
        }
    }
}
