using System;

namespace Game
{
    public class P002GameSoundRuntime : IP002GameSound
    {
        const int INITIAL_SOUND_INDEX = -1;

        readonly P002GameSoundConfig _config;

        bool _isEnabled = true;
        int _lastPlayedSoundIndex = INITIAL_SOUND_INDEX;
        float _enemyHitChance;

        public event Action<int> OnSoundRequested;

        public bool IsEnabled => _isEnabled;

        public int LastPlayedSoundIndex => _lastPlayedSoundIndex;

        public P002GameSoundRuntime(P002GameSoundConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _enemyHitChance = _config.EnemyHitChance;
            _lastPlayedSoundIndex = INITIAL_SOUND_INDEX;
        }

        public void Release()
        {
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        void FireSoundRequest(int index)
        {
            _lastPlayedSoundIndex = index;
            if (OnSoundRequested != null)
            {
                OnSoundRequested(index);
            }
        }

        public void PlayBlockDrop()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_BLOCK_DROP);
            }
        }

        public void PlayBlockDestroy()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_BLOCK_DESTROY);
            }
        }

        public void PlaySwapSuccess()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_SWAP_SUCCESS);
            }
        }

        public void PlaySwapFail()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_SWAP_FAIL);
            }
        }

        public void PlayBombActivate()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_BOMB_ACTIVATE);
            }
        }

        public void PlayColorClear()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_COLOR_CLEAR);
            }
        }

        public void PlayAttack(int weaponIndex)
        {
            if (_isEnabled)
            {
                int index = P002GameSoundConfig.SOUND_ATTACK_BOMB + weaponIndex;
                FireSoundRequest(index);
            }
        }

        public void PlaySkill(int weaponIndex)
        {
            if (_isEnabled)
            {
                int index;
                if (weaponIndex == 0) index = P002GameSoundConfig.SOUND_SKILL_BOMB;
                else if (weaponIndex == 1) index = P002GameSoundConfig.SOUND_SKILL_SWORD;
                else if (weaponIndex == 2) index = P002GameSoundConfig.SOUND_SKILL_WAND;
                else index = P002GameSoundConfig.SOUND_SKILL_BOMB + weaponIndex;
                FireSoundRequest(index);
            }
        }

        public void TryPlayEnemyHit()
        {
            if (_isEnabled && UnityEngine.Random.value < _enemyHitChance)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_ENEMY_HIT);
            }
        }

        public void PlayEnemyDeath()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_ENEMY_DEATH);
            }
        }

        public void PlayVictory()
        {
            if (_isEnabled)
            {
                FireSoundRequest(P002GameSoundConfig.SOUND_VICTORY);
            }
        }
    }
}
