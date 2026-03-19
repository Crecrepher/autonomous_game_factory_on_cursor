using System;
using UnityEngine;

namespace Game
{
    public class P002BattleMonsterRuntime : IP002BattleMonster
    {
        const int DEFAULT_MONSTER_HEALTH = 300;

        public event Action<int> OnMonsterDefeated;
        public event Action OnAllMonstersDefeated;
        public event Action<int, int, int> OnMonsterHealthChanged;

        int _monsterCount;
        int _currentMonsterIndex;
        int[] _monsterMaxHealth;
        int[] _monsterCurrentHealth;
        bool[] _monsterIsDead;
        float[] _inverseMaxHealth;
        int _skillDamageMin;
        int _skillDamageMax;
        int _basicDamageMin;
        int _basicDamageMax;
        bool _enableBasicAttack;

        public int CurrentMonsterIndex => _currentMonsterIndex;
        public bool IsAllMonstersDead => _currentMonsterIndex >= _monsterCount;

        public void Init(IP002GameConfig config)
        {
            _monsterCount = config.MonsterCount;
            _currentMonsterIndex = 0;
            _skillDamageMin = config.SkillDamageMin;
            _skillDamageMax = config.SkillDamageMax;
            _basicDamageMin = config.BasicDamageMin;
            _basicDamageMax = config.BasicDamageMax;
            _enableBasicAttack = config.EnableBasicAttack;

            _monsterMaxHealth = new int[_monsterCount];
            _monsterCurrentHealth = new int[_monsterCount];
            _monsterIsDead = new bool[_monsterCount];
            _inverseMaxHealth = new float[_monsterCount];

            for (int i = 0; i < _monsterCount; i++)
            {
                int hp = config.GetMonsterHealth(i);
                _monsterMaxHealth[i] = hp;
                _monsterCurrentHealth[i] = hp;
                _monsterIsDead[i] = false;
                _inverseMaxHealth[i] = hp > 0 ? 1f / hp : 0f;
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void ApplyBasicAttack(int characterIndex)
        {
            if (!_enableBasicAttack) return;
            if (IsAllMonstersDead) return;

            int damage = UnityEngine.Random.Range(_basicDamageMin, _basicDamageMax + 1);
            ApplyDamage(damage);
        }

        public void ApplySkillAttack(int characterIndex)
        {
            if (IsAllMonstersDead) return;

            int damage = UnityEngine.Random.Range(_skillDamageMin, _skillDamageMax + 1);
            ApplyDamage(damage);
        }

        public void ApplySkillDamageChunk(int characterIndex, int damage)
        {
            if (IsAllMonstersDead) return;
            ApplyDamage(damage);
        }

        public int GetSkillDamage()
        {
            return UnityEngine.Random.Range(_skillDamageMin, _skillDamageMax + 1);
        }

        public int GetCurrentMonsterHealth()
        {
            if (_currentMonsterIndex >= _monsterCount) return 0;
            return _monsterCurrentHealth[_currentMonsterIndex];
        }

        public float GetCurrentMonsterHealthRatio()
        {
            if (_currentMonsterIndex >= _monsterCount) return 0f;
            return _monsterCurrentHealth[_currentMonsterIndex] * _inverseMaxHealth[_currentMonsterIndex];
        }

        void ApplyDamage(int damage)
        {
            if (_currentMonsterIndex >= _monsterCount) return;

            _monsterCurrentHealth[_currentMonsterIndex] -= damage;
            if (_monsterCurrentHealth[_currentMonsterIndex] < 0)
            {
                _monsterCurrentHealth[_currentMonsterIndex] = 0;
            }

            if (OnMonsterHealthChanged != null)
            {
                OnMonsterHealthChanged.Invoke(
                    _currentMonsterIndex,
                    _monsterCurrentHealth[_currentMonsterIndex],
                    _monsterMaxHealth[_currentMonsterIndex]);
            }

            if (_monsterCurrentHealth[_currentMonsterIndex] <= 0)
            {
                _monsterIsDead[_currentMonsterIndex] = true;
                if (OnMonsterDefeated != null)
                {
                    OnMonsterDefeated.Invoke(_currentMonsterIndex);
                }

                _currentMonsterIndex++;

                if (_currentMonsterIndex >= _monsterCount)
                {
                    if (OnAllMonstersDefeated != null)
                    {
                        OnAllMonstersDefeated.Invoke();
                    }
                }
            }
        }
    }
}
