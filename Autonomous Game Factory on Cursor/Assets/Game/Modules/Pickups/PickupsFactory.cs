namespace Game
{
    public static class PickupsFactory
    {
        public static IPickups CreateRuntime(PickupsConfig config)
        {
            return new PickupsRuntime(config);
        }
    }
}
