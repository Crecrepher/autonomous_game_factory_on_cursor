namespace Game
{
    public static class P002BattleCharacterFactory
    {
        public static IP002BattleCharacter Create(P002BattleCharacterConfig config)
        {
            return new P002BattleCharacterRuntime(config);
        }
    }
}
