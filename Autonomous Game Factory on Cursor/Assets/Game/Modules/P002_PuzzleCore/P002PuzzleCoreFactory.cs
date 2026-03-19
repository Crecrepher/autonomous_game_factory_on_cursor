namespace Game
{
    public static class P002PuzzleCoreFactory
    {
        public static IP002PuzzleCore Create(P002PuzzleCoreConfig config)
        {
            if (config == null) return null;
            return new P002PuzzleCoreRuntime(config);
        }
    }
}
