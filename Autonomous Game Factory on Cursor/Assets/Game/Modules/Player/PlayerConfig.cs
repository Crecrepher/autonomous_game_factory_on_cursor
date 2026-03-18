using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Modules/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [SerializeField] float _moveSpeed = 5f;
        [SerializeField] float _attackSpeed = 1f;
        [SerializeField] int _attackDamage = 1;
        [SerializeField] float _attackRange = 3f;

        public float MoveSpeed => _moveSpeed;
        public float AttackSpeed => _attackSpeed;
        public int AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
    }
}
