namespace Game
{
    public static class P002SkillSystemFactory
    {
        public static IP002SkillSystem Create(P002SkillSystemConfig config)
        {
            return new P002SkillSystemRuntime(config);
        }
    }
}
