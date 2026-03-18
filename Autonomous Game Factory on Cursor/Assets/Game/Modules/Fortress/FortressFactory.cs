namespace Game
{
    public static class FortressFactory
    {
        public static IFortress CreateRuntime(FortressConfig config)
        {
            return new FortressRuntime(config);
        }
    }
}
