namespace Game
{
    public static class BuffIconUIFactory
    {
        public static IBuffIconUI CreateRuntime(BuffIconUIConfig config)
        {
            return new BuffIconUIRuntime(config);
        }
    }
}
