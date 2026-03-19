using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002BattleCharacterConfig", menuName = "Game/Modules/P002BattleCharacterConfig")]
    public class P002BattleCharacterConfig : ScriptableObject
    {
        public const int CHARACTER_COUNT = 3;

        const int DEFAULT_SKILL_GAUGE_MAX = 50;
        const int DEFAULT_GAUGE_PER_PIECE = 10;

        [Header("Skill Gauge")]
        [SerializeField] int _skillGaugeMax = DEFAULT_SKILL_GAUGE_MAX;
        [SerializeField] int _gaugePerPiece = DEFAULT_GAUGE_PER_PIECE;
        [SerializeField] int[] _initialGaugePerCharacter = { 20, 20, 20 };

        [Header("Weapon Types")]
        [SerializeField] EP002WeaponType[] _weaponTypes = { EP002WeaponType.Sword, EP002WeaponType.Bow, EP002WeaponType.Staff };

        public int SkillGaugeMax => _skillGaugeMax;
        public int GaugePerPiece => _gaugePerPiece;

        public int GetInitialGauge(int characterIndex)
        {
            if (_initialGaugePerCharacter == null || characterIndex < 0 || characterIndex >= _initialGaugePerCharacter.Length)
                return DEFAULT_GAUGE_PER_PIECE;
            return _initialGaugePerCharacter[characterIndex];
        }

        public EP002WeaponType GetWeaponType(int characterIndex)
        {
            if (_weaponTypes == null || characterIndex < 0 || characterIndex >= _weaponTypes.Length)
                return EP002WeaponType.None;
            return _weaponTypes[characterIndex];
        }
    }
}
