using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002GameSoundConfig", menuName = "Game/Modules/P002GameSoundConfig")]
    public class P002GameSoundConfig : ScriptableObject
    {
        public const int SOUND_BLOCK_DROP = 0;
        public const int SOUND_BLOCK_DESTROY = 1;
        public const int SOUND_SWAP_SUCCESS = 2;
        public const int SOUND_SWAP_FAIL = 3;
        public const int SOUND_BOMB_ACTIVATE = 4;
        public const int SOUND_COLOR_CLEAR = 5;
        public const int SOUND_ATTACK_BOMB = 6;
        public const int SOUND_ATTACK_SWORD = 7;
        public const int SOUND_ATTACK_WAND = 8;
        public const int SOUND_SKILL_SWORD = 9;
        public const int SOUND_SKILL_BOMB = 10;
        public const int SOUND_SKILL_WAND = 11;
        public const int SOUND_ENEMY_HIT = 12;
        public const int SOUND_ENEMY_DEATH = 13;
        public const int SOUND_VICTORY = 14;
        public const int TOTAL_SOUND_COUNT = 15;

        const float DEFAULT_ENEMY_HIT_CHANCE = 0.35f;

        [SerializeField] float _enemyHitChance = DEFAULT_ENEMY_HIT_CHANCE;

        public float EnemyHitChance => _enemyHitChance;
    }
}
