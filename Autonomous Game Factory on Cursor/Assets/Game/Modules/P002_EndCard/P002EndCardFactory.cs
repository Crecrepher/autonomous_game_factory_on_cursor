namespace Game
{
    public static class P002EndCardFactory
    {
        public static IP002EndCard Create(P002EndCardConfig config)
        {
            return new P002EndCardRuntime(config);
        }
    }
}
