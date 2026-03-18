namespace Game
{
    public static class PlayerFactory
    {
        public static IPlayer CreateRuntime(PlayerConfig config)
        {
            return new PlayerRuntime(config);
        }
    }
}
