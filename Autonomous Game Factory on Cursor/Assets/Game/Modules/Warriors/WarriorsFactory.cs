namespace Game
{
    public static class WarriorsFactory
    {
        public static IWarriors CreateRuntime(WarriorsConfig config)
        {
            return new WarriorsRuntime(config);
        }
    }
}
