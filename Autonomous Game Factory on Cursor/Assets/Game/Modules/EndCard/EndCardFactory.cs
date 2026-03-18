namespace Game
{
    public static class EndCardFactory
    {
        public static IEndCard CreateRuntime(EndCardConfig config)
        {
            return new EndCardRuntime(config);
        }
    }
}
