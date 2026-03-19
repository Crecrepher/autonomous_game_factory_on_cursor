using System;

namespace Game
{
    public interface IP002BattleEffect
    {
        int ActiveProjectileCount { get; }
        event Action<int, float, float, float, float, int> OnProjectileLaunchRequested;
        event Action<float, float> OnHitEffectRequested;
        event Action<float, float, int> OnParticleRequested;
        event Action<float, float> OnCameraShakeRequested;
        event Action<int> OnProjectileArrived;

        void Init(int projectilePoolSize, int hitEffectPoolSize, int particlePoolSize);
        void Tick(float deltaTime);
        void LaunchProjectile(int projectileType, float startX, float startY, float targetX, float targetY, int characterIndex);
        void PlayHitEffect(float x, float y);
        void PlayBlockMatchParticle(float x, float y, int blockType);
        void ShakeCamera(float duration, float magnitude);
    }
}
