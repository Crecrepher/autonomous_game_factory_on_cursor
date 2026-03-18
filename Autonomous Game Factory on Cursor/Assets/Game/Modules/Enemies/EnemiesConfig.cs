using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/Modules/EnemiesConfig")]
    public class EnemiesConfig : ScriptableObject
    {
        [SerializeField] int _normalEnemyHp = 4;
        [SerializeField] int _normalEnemyCoinDrop = 1;
        [SerializeField] int _bigEnemyHp = 20;
        [SerializeField] int _bigEnemyCoinDrop = 30;
        [SerializeField] float _spawnInterval = 2f;
        [SerializeField] int _maxAliveEnemies = 20;

        public int NormalEnemyHp => _normalEnemyHp;
        public int NormalEnemyCoinDrop => _normalEnemyCoinDrop;
        public int BigEnemyHp => _bigEnemyHp;
        public int BigEnemyCoinDrop => _bigEnemyCoinDrop;
        public float SpawnInterval => _spawnInterval;
        public int MaxAliveEnemies => _maxAliveEnemies;
    }
}
