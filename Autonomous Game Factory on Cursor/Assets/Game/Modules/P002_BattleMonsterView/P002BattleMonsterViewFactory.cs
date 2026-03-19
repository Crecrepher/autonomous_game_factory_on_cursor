namespace Game
{
    public static class P002BattleMonsterViewFactory
    {
        public static IP002BattleMonsterView Create(P002BattleMonsterViewConfig config)
        {
            return new P002BattleMonsterViewRuntime();
        }
    }
}
