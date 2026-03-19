using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002BattleMonsterConfig", menuName = "Game/Modules/P002BattleMonsterConfig")]
    public class P002BattleMonsterConfig : ScriptableObject
    {
        const int DEFAULT_MONSTER_HEALTH_0 = 300;
        const int DEFAULT_MONSTER_HEALTH_1 = 500;
        const int DEFAULT_SKILL_DAMAGE_MIN = 40;
        const int DEFAULT_SKILL_DAMAGE_MAX = 60;
        const int DEFAULT_BASIC_DAMAGE_MIN = 15;
        const int DEFAULT_BASIC_DAMAGE_MAX = 25;

        [SerializeField] int[] _monsterHealthList = { DEFAULT_MONSTER_HEALTH_0, DEFAULT_MONSTER_HEALTH_1 };
        [SerializeField] int _skillDamageMin = DEFAULT_SKILL_DAMAGE_MIN;
        [SerializeField] int _skillDamageMax = DEFAULT_SKILL_DAMAGE_MAX;
        [SerializeField] int _basicDamageMin = DEFAULT_BASIC_DAMAGE_MIN;
        [SerializeField] int _basicDamageMax = DEFAULT_BASIC_DAMAGE_MAX;
        [SerializeField] bool _enableBasicAttack = true;

        public int[] MonsterHealthList => _monsterHealthList;
        public int SkillDamageMin => _skillDamageMin;
        public int SkillDamageMax => _skillDamageMax;
        public int BasicDamageMin => _basicDamageMin;
        public int BasicDamageMax => _basicDamageMax;
        public bool EnableBasicAttack => _enableBasicAttack;
    }
}
