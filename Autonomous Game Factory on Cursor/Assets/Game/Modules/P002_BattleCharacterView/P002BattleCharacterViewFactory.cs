namespace Game
{
    public static class P002BattleCharacterViewFactory
    {
        public static IP002BattleCharacterView Create(P002BattleCharacterViewConfig config)
        {
            return new P002BattleCharacterViewRuntime();
        }
    }
}
