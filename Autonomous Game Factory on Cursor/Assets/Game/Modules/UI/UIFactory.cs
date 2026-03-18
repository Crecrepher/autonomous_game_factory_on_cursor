namespace Game
{
    public static class UIFactory
    {
        public static IUI CreateRuntime(UIConfig config)
        {
            return new UIRuntime(config);
        }
    }
}
