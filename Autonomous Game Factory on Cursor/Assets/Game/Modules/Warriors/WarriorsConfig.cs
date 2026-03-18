using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "WarriorsConfig", menuName = "Game/Modules/WarriorsConfig")]
    public class WarriorsConfig : ScriptableObject
    {
        [SerializeField] int _maxWarriors = 26;
        [SerializeField] int _hireCostBase = 2;
        [SerializeField] int _hireCostIncrement = 2;
        [SerializeField] int _hireCostMax = 10;
        [SerializeField] int _attackDamage = 1;
        [SerializeField] float _attackSpeed = 1f;
        [SerializeField] float _moveSpeed = 3f;

        public int MaxWarriors => _maxWarriors;
        public int HireCostBase => _hireCostBase;
        public int HireCostIncrement => _hireCostIncrement;
        public int HireCostMax => _hireCostMax;
        public int AttackDamage => _attackDamage;
        public float AttackSpeed => _attackSpeed;
        public float MoveSpeed => _moveSpeed;
    }
}
