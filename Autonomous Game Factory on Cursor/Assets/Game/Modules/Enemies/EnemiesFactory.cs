namespace Game
{
    public static class EnemiesFactory
    {
        public static IEnemies CreateRuntime(EnemiesConfig config)
        {
            return new EnemiesRuntime(config);
        }
    }
}
