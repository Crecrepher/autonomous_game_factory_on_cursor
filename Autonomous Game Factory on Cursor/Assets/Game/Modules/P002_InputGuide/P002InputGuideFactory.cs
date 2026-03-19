namespace Game
{
    public static class P002InputGuideFactory
    {
        public static IP002InputGuide Create(P002InputGuideConfig config)
        {
            if (config == null)
                return null;
            return new P002InputGuideRuntime(config);
        }
    }
}
