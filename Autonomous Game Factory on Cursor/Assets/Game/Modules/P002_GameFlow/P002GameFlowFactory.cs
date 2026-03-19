namespace Game
{
    public static class P002GameFlowFactory
    {
        public static IP002GameFlow Create(P002GameFlowConfig config)
        {
            var runtime = new P002GameFlowRuntime();
            runtime.Init();
            runtime.Configure(config.TotalStages, config.StageTransitionDelay);
            return runtime;
        }
    }
}
