using System;

namespace Game
{
    public class PlayerRuntime : IPlayer
    {
        const float MIN_SPEED = 0f;
        const float MIN_ATTACK_SPEED = 0.01f;

        public event Action OnAttack;

        public float MoveSpeed => _moveSpeed;
        public float AttackSpeed => _attackSpeed;
        public int AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public bool IsAttacking => _isAttacking;

        readonly PlayerConfig _config;
        float _moveSpeed;
        float _attackSpeed;
        int _attackDamage;
        float _attackRange;
        float _sqrAttackRange;
        float _attackInterval;
        float _attackTimer;
        float _moveDirX;
        float _moveDirY;
        bool _isAttacking;

        public PlayerRuntime(PlayerConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _moveSpeed = _config.MoveSpeed;
            _attackSpeed = _config.AttackSpeed;
            _attackDamage = _config.AttackDamage;
            _attackRange = _config.AttackRange;
            _sqrAttackRange = _attackRange * _attackRange;
            _attackInterval = 1f / _attackSpeed;
            _attackTimer = 0f;
            _moveDirX = 0f;
            _moveDirY = 0f;
            _isAttacking = false;
        }

        public void Tick(float deltaTime)
        {
            if (_attackTimer > 0f)
            {
                _attackTimer -= deltaTime;
            }
        }

        public void SetMoveDirection(float x, float y)
        {
            _moveDirX = x;
            _moveDirY = y;
        }

        public void SetMoveSpeed(float speed)
        {
            if (speed < MIN_SPEED)
                speed = MIN_SPEED;

            _moveSpeed = speed;
        }

        public void SetAttackSpeed(float speed)
        {
            if (speed < MIN_ATTACK_SPEED)
                speed = MIN_ATTACK_SPEED;

            _attackSpeed = speed;
            _attackInterval = 1f / _attackSpeed;
        }

        public bool TryAttack()
        {
            if (_attackTimer > 0f)
                return false;

            _attackTimer = _attackInterval;
            _isAttacking = true;

            if (OnAttack != null)
                OnAttack.Invoke();

            return true;
        }

        public void EndAttack()
        {
            _isAttacking = false;
        }

        public float GetSqrAttackRange()
        {
            return _sqrAttackRange;
        }

        public float GetMoveDirX()
        {
            return _moveDirX;
        }

        public float GetMoveDirY()
        {
            return _moveDirY;
        }
    }
}
