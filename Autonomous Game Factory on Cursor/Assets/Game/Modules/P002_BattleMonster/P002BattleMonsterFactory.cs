namespace Game
{
    public static class P002BattleMonsterFactory
    {
        public static IP002BattleMonster Create()
        {
            return new P002BattleMonsterRuntime();
        }
    }
}
