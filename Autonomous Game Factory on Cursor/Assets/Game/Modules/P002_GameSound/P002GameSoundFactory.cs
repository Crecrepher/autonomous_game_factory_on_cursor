namespace Game
{
    public static class P002GameSoundFactory
    {
        public static IP002GameSound Create(P002GameSoundConfig config)
        {
            return new P002GameSoundRuntime(config);
        }
    }
}
