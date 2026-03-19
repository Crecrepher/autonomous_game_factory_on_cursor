namespace Game
{
    public static class P002PuzzleViewFactory
    {
        public static IP002PuzzleView Create(P002PuzzleViewConfig config)
        {
            if (config == null)
                return null;
            return new P002PuzzleViewRuntime(config);
        }
    }
}
