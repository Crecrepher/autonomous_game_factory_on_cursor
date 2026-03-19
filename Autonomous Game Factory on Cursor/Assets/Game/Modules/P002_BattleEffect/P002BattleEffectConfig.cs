using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002BattleEffectConfig", menuName = "Game/Modules/P002BattleEffectConfig")]
    public class P002BattleEffectConfig : ScriptableObject
    {
        public const int DEFAULT_PROJECTILE_POOL = 5;
        public const int DEFAULT_HIT_POOL = 3;
        public const int DEFAULT_PARTICLE_POOL = 6;

        [SerializeField] float _projectileSpeed = 15f;
        [SerializeField] float _cameraShakeDuration = 0.15f;
        [SerializeField] float _cameraShakeMagnitude = 0.1f;
        [SerializeField] float _skillShakeDuration = 0.25f;
        [SerializeField] float _skillShakeMagnitude = 0.2f;

        public float ProjectileSpeed => _projectileSpeed;
        public float CameraShakeDuration => _cameraShakeDuration;
        public float CameraShakeMagnitude => _cameraShakeMagnitude;
        public float SkillShakeDuration => _skillShakeDuration;
        public float SkillShakeMagnitude => _skillShakeMagnitude;
    }
}
