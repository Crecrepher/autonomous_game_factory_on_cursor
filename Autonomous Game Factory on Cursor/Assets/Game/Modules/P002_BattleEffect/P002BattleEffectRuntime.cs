using System;

namespace Game
{
    public struct ProjectileState
    {
        public bool Active { get; set; }
        public int Type { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float Elapsed { get; set; }
        public float Duration { get; set; }
        public int CharacterIndex { get; set; }
    }

    public class P002BattleEffectRuntime : IP002BattleEffect
    {
        public event Action<int, float, float, float, float, int> OnProjectileLaunchRequested;
        public event Action<float, float> OnHitEffectRequested;
        public event Action<float, float, int> OnParticleRequested;
        public event Action<float, float> OnCameraShakeRequested;
        public event Action<int> OnProjectileArrived;

        const float MIN_DURATION = 0.01f;

        ProjectileState[] _projectiles;
        int _activeCount;
        int _poolSize;
        float _projectileSpeed;

        public int ActiveProjectileCount => _activeCount;

        public void Init(int projectilePoolSize, int hitEffectPoolSize, int particlePoolSize)
        {
            _poolSize = projectilePoolSize;
            _projectiles = new ProjectileState[projectilePoolSize];
            _activeCount = 0;

            for (int i = 0; i < projectilePoolSize; i++)
            {
                _projectiles[i].Active = false;
            }
        }

        public void SetProjectileSpeed(float speed)
        {
            _projectileSpeed = speed;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _poolSize; i++)
            {
                if (_projectiles[i].Active == false)
                    continue;

                ProjectileState s = _projectiles[i];
                s.Elapsed += deltaTime;

                if (s.Elapsed >= s.Duration)
                {
                    s.Active = false;
                    _activeCount--;
                    if (_activeCount < 0) _activeCount = 0;

                    int charIndex = s.CharacterIndex;
                    _projectiles[i] = s;

                    if (OnProjectileArrived != null)
                        OnProjectileArrived.Invoke(charIndex);
                }
                else
                {
                    _projectiles[i] = s;
                }
            }
        }

        public void LaunchProjectile(int projectileType, float startX, float startY, float targetX, float targetY, int characterIndex)
        {
            float dx = targetX - startX;
            float dy = targetY - startY;
            float dist = (float)Math.Sqrt((double)(dx * dx + dy * dy));
            float duration = dist / _projectileSpeed;
            if (duration < MIN_DURATION) duration = MIN_DURATION;

            for (int i = 0; i < _poolSize; i++)
            {
                if (_projectiles[i].Active == false)
                {
                    _projectiles[i].Active = true;
                    _projectiles[i].Type = projectileType;
                    _projectiles[i].StartX = startX;
                    _projectiles[i].StartY = startY;
                    _projectiles[i].TargetX = targetX;
                    _projectiles[i].TargetY = targetY;
                    _projectiles[i].Elapsed = 0f;
                    _projectiles[i].Duration = duration;
                    _projectiles[i].CharacterIndex = characterIndex;
                    _activeCount++;

                    if (OnProjectileLaunchRequested != null)
                        OnProjectileLaunchRequested.Invoke(projectileType, startX, startY, targetX, targetY, characterIndex);
                    return;
                }
            }
        }

        public void PlayHitEffect(float x, float y)
        {
            if (OnHitEffectRequested != null)
                OnHitEffectRequested.Invoke(x, y);
        }

        public void PlayBlockMatchParticle(float x, float y, int blockType)
        {
            if (OnParticleRequested != null)
                OnParticleRequested.Invoke(x, y, blockType);
        }

        public void ShakeCamera(float duration, float magnitude)
        {
            if (OnCameraShakeRequested != null)
                OnCameraShakeRequested.Invoke(duration, magnitude);
        }
    }
}
