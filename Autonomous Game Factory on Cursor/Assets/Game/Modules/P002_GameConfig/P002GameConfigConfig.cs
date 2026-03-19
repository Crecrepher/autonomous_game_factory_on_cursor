using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002GameConfigConfig", menuName = "Game/Modules/P002GameConfigConfig")]
    public class P002GameConfigConfig : ScriptableObject
    {
        const int DEFAULT_BOARD_WIDTH = 6;
        const int DEFAULT_BOARD_HEIGHT = 5;
        const int DEFAULT_SKILL_GAUGE_MAX = 50;
        const int DEFAULT_GAUGE_PER_PIECE = 10;
        const int DEFAULT_SKILL_DAMAGE_MIN = 40;
        const int DEFAULT_SKILL_DAMAGE_MAX = 60;
        const int DEFAULT_BASIC_DAMAGE_MIN = 15;
        const int DEFAULT_BASIC_DAMAGE_MAX = 25;
        const float DEFAULT_IDLE_GUIDE_DELAY = 3f;
        const float DEFAULT_INACTIVITY_TIMEOUT = 30f;
        const int DEFAULT_SKILL_BASIC_ATTACK_COUNT = 5;
        const float DEFAULT_SKILL_BASIC_ATTACK_INTERVAL = 0.15f;
        const float DEFAULT_AUTO_SKILL_DELAY = 0.5f;
        const int DEFAULT_INITIAL_GAUGE = 20;
        const int DEFAULT_MONSTER_HEALTH = 300;

        [Header("Board")]
        [SerializeField] EP002PuzzleBoardSize _puzzleBoardSize = EP002PuzzleBoardSize.Size6x5;

        [Header("Monster")]
        [SerializeField] int[] _monsterHealthList = { 300, 500 };

        [Header("Skill Gauge")]
        [SerializeField] int _skillGaugeMax = DEFAULT_SKILL_GAUGE_MAX;
        [SerializeField] int _gaugePerPiece = DEFAULT_GAUGE_PER_PIECE;
        [SerializeField] int[] _initialGaugePerCharacter = { 20, 20, 20 };

        [Header("Damage")]
        [SerializeField] int _skillDamageMin = DEFAULT_SKILL_DAMAGE_MIN;
        [SerializeField] int _skillDamageMax = DEFAULT_SKILL_DAMAGE_MAX;
        [SerializeField] int _basicDamageMin = DEFAULT_BASIC_DAMAGE_MIN;
        [SerializeField] int _basicDamageMax = DEFAULT_BASIC_DAMAGE_MAX;

        [Header("Input Guide")]
        [SerializeField] float _idleGuideDelay = DEFAULT_IDLE_GUIDE_DELAY;
        [SerializeField] float _inactivityTimeout = DEFAULT_INACTIVITY_TIMEOUT;

        [Header("Variation")]
        [SerializeField] bool _enableBasicAttack = true;
        [SerializeField] EP002SkillMode _skillMode = EP002SkillMode.Manual;
        [SerializeField] bool _enableSkillAnimation = true;

        [Header("Special Block")]
        [SerializeField] bool _enableBombBlock = true;
        [SerializeField] bool _enableColorClearBlock = true;

        [Header("Projectile")]
        [SerializeField] bool _projectileStartFromCharacter;

        [Header("Skill Attack")]
        [SerializeField] EP002SkillAttackMode _skillAttackMode = EP002SkillAttackMode.Normal;
        [SerializeField] int _skillBasicAttackCount = DEFAULT_SKILL_BASIC_ATTACK_COUNT;
        [SerializeField] float _skillBasicAttackInterval = DEFAULT_SKILL_BASIC_ATTACK_INTERVAL;

        [Header("Auto Skill")]
        [SerializeField] float _autoSkillDelay = DEFAULT_AUTO_SKILL_DELAY;

        public EP002PuzzleBoardSize PuzzleBoardSize => _puzzleBoardSize;

        public int BoardWidth
        {
            get
            {
                if (_puzzleBoardSize == EP002PuzzleBoardSize.Size5x4) return 5;
                return DEFAULT_BOARD_WIDTH;
            }
        }

        public int BoardHeight
        {
            get
            {
                if (_puzzleBoardSize == EP002PuzzleBoardSize.Size5x4) return 4;
                return DEFAULT_BOARD_HEIGHT;
            }
        }

        public int MonsterCount => _monsterHealthList != null ? _monsterHealthList.Length : 0;

        public int GetMonsterHealth(int index)
        {
            if (_monsterHealthList == null || index < 0 || index >= _monsterHealthList.Length)
                return DEFAULT_MONSTER_HEALTH;
            return _monsterHealthList[index];
        }

        public int SkillGaugeMax => _skillGaugeMax;
        public int GaugePerPiece => _gaugePerPiece;

        public int GetInitialGauge(int characterIndex)
        {
            if (_initialGaugePerCharacter == null || characterIndex < 0 || characterIndex >= _initialGaugePerCharacter.Length)
                return DEFAULT_INITIAL_GAUGE;
            return _initialGaugePerCharacter[characterIndex];
        }

        public int SkillDamageMin => _skillDamageMin;
        public int SkillDamageMax => _skillDamageMax;
        public int BasicDamageMin => _basicDamageMin;
        public int BasicDamageMax => _basicDamageMax;
        public float IdleGuideDelay => _idleGuideDelay;
        public float InactivityTimeout => _inactivityTimeout;
        public bool EnableBasicAttack => _enableBasicAttack;
        public EP002SkillMode SkillMode => _skillMode;
        public bool EnableSkillAnimation => _enableSkillAnimation;
        public bool EnableBombBlock => _enableBombBlock;
        public bool EnableColorClearBlock => _enableColorClearBlock;
        public bool ProjectileStartFromCharacter => _projectileStartFromCharacter;
        public EP002SkillAttackMode SkillAttackMode => _skillAttackMode;
        public bool UseBasicAttackForSkill => _skillAttackMode == EP002SkillAttackMode.BasicOnly;
        public int SkillBasicAttackCount => _skillBasicAttackCount;
        public float SkillBasicAttackInterval => _skillBasicAttackInterval;
        public float AutoSkillDelay => _autoSkillDelay;
    }
}
