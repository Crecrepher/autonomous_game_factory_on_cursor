namespace Game
{
    public static class P002GameEventsFactory
    {
        public static IP002GameEvents Create(P002GameEventsConfig config)
        {
            return new P002GameEventsRuntime();
        }
    }
}
