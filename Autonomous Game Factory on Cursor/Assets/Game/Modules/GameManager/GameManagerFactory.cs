namespace Game
{
    public static class GameManagerFactory
    {
        public static IGameManager CreateRuntime(GameManagerConfig config)
        {
            return new GameManagerRuntime(config);
        }
    }
}
