namespace Game
{
    public static class HireNodesFactory
    {
        public static IHireNodes CreateRuntime(HireNodesConfig config)
        {
            return new HireNodesRuntime(config);
        }
    }
}
