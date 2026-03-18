namespace Game
{
    public static class DefenseTowersFactory
    {
        public static IDefenseTowers CreateRuntime(DefenseTowersConfig config)
        {
            return new DefenseTowersRuntime(config);
        }
    }
}
