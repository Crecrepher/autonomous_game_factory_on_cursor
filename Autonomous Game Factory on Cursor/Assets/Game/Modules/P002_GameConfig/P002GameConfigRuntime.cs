namespace Game
{
    public class P002GameConfigRuntime : IP002GameConfig
    {
        readonly P002GameConfigConfig _config;

        public int BoardWidth => _config.BoardWidth;
        public int BoardHeight => _config.BoardHeight;
        public int MonsterCount => _config.MonsterCount;
        public int SkillGaugeMax => _config.SkillGaugeMax;
        public int GaugePerPiece => _config.GaugePerPiece;
        public int SkillDamageMin => _config.SkillDamageMin;
        public int SkillDamageMax => _config.SkillDamageMax;
        public int BasicDamageMin => _config.BasicDamageMin;
        public int BasicDamageMax => _config.BasicDamageMax;
        public float IdleGuideDelay => _config.IdleGuideDelay;
        public float InactivityTimeout => _config.InactivityTimeout;
        public bool EnableBasicAttack => _config.EnableBasicAttack;
        public bool EnableSkillAnimation => _config.EnableSkillAnimation;
        public bool EnableBombBlock => _config.EnableBombBlock;
        public bool EnableColorClearBlock => _config.EnableColorClearBlock;
        public bool ProjectileStartFromCharacter => _config.ProjectileStartFromCharacter;
        public int SkillBasicAttackCount => _config.SkillBasicAttackCount;
        public float SkillBasicAttackInterval => _config.SkillBasicAttackInterval;
        public float AutoSkillDelay => _config.AutoSkillDelay;
        public bool UseBasicAttackForSkill => _config.UseBasicAttackForSkill;
        public EP002SkillMode SkillMode => _config.SkillMode;
        public EP002SkillAttackMode SkillAttackMode => _config.SkillAttackMode;

        public P002GameConfigRuntime(P002GameConfigConfig config)
        {
            _config = config;
        }

        public int GetMonsterHealth(int index) => _config.GetMonsterHealth(index);
        public int GetInitialGauge(int characterIndex) => _config.GetInitialGauge(characterIndex);
    }
}
