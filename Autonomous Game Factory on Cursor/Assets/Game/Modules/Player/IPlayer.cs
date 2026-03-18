namespace Game
{
    public interface IPlayer
    {
        void Init();
        void Tick(float deltaTime);

        float MoveSpeed { get; }
        float AttackSpeed { get; }
        int AttackDamage { get; }
        float AttackRange { get; }
        bool IsAttacking { get; }

        void SetMoveDirection(float x, float y);
        void SetMoveSpeed(float speed);
        void SetAttackSpeed(float speed);

        event System.Action OnAttack;
    }
}
