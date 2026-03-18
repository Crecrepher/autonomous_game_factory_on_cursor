namespace Game
{
    public static class BlacksmithFactory
    {
        public static IBlacksmith CreateRuntime(BlacksmithConfig config)
        {
            return new BlacksmithRuntime(config);
        }
    }
}
