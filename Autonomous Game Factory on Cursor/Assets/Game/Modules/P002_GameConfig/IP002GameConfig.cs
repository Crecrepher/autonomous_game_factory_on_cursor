namespace Game
{
    public interface IP002GameConfig
    {
        int BoardWidth { get; }
        int BoardHeight { get; }
        int MonsterCount { get; }
        int GetMonsterHealth(int index);
        int SkillGaugeMax { get; }
        int GaugePerPiece { get; }
        int GetInitialGauge(int characterIndex);
        int SkillDamageMin { get; }
        int SkillDamageMax { get; }
        int BasicDamageMin { get; }
        int BasicDamageMax { get; }
        float IdleGuideDelay { get; }
        float InactivityTimeout { get; }
        bool EnableBasicAttack { get; }
        bool EnableSkillAnimation { get; }
        bool EnableBombBlock { get; }
        bool EnableColorClearBlock { get; }
        bool ProjectileStartFromCharacter { get; }
        int SkillBasicAttackCount { get; }
        float SkillBasicAttackInterval { get; }
        float AutoSkillDelay { get; }
        bool UseBasicAttackForSkill { get; }
        EP002SkillMode SkillMode { get; }
        EP002SkillAttackMode SkillAttackMode { get; }
    }
}
