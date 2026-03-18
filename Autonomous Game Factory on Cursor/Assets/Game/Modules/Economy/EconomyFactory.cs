namespace Game
{
    public static class EconomyFactory
    {
        public static IEconomy CreateRuntime(EconomyConfig config)
        {
            return new EconomyRuntime(config);
        }
    }
}
