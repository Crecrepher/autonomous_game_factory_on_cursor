namespace Game
{
    public static class GuideFactory
    {
        public static IGuide CreateRuntime(GuideConfig config)
        {
            return new GuideRuntime(config);
        }
    }
}
