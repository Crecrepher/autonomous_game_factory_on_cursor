namespace Game
{
    public static class P002BattleEffectFactory
    {
        public static IP002BattleEffect Create(P002BattleEffectConfig config)
        {
            var runtime = new P002BattleEffectRuntime();
            runtime.Init(
                P002BattleEffectConfig.DEFAULT_PROJECTILE_POOL,
                P002BattleEffectConfig.DEFAULT_HIT_POOL,
                P002BattleEffectConfig.DEFAULT_PARTICLE_POOL);
            runtime.SetProjectileSpeed(config.ProjectileSpeed);
            return runtime;
        }
    }
}
