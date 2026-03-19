namespace Game
{
    public static class P002GameConfigFactory
    {
        public static IP002GameConfig Create(P002GameConfigConfig config)
        {
            return new P002GameConfigRuntime(config);
        }
    }
}
