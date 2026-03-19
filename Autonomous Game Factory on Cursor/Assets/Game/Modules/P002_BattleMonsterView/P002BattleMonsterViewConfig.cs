using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002BattleMonsterViewConfig", menuName = "Game/Modules/P002BattleMonsterViewConfig")]
    public class P002BattleMonsterViewConfig : ScriptableObject
    {
        [SerializeField] float _hitFlashDuration = 0.1f;
        [SerializeField] float _deathAnimDuration = 0.5f;
        [SerializeField] float _spawnAnimDuration = 0.3f;
        [SerializeField] float _damageTextLifetime = 1.0f;

        public float HitFlashDuration => _hitFlashDuration;
        public float DeathAnimDuration => _deathAnimDuration;
        public float SpawnAnimDuration => _spawnAnimDuration;
        public float DamageTextLifetime => _damageTextLifetime;
    }
}
