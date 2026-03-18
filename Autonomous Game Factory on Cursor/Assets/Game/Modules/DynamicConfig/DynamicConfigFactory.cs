namespace Game
{
    public static class DynamicConfigFactory
    {
        public static IDynamicConfig CreateRuntime(DynamicConfigConfig config)
        {
            return new DynamicConfigRuntime(config);
        }
    }
}
