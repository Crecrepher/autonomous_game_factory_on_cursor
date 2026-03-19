using UnityEngine;

namespace Game
{
    public class P002BattleMonsterBootstrap : MonoBehaviour
    {
        [SerializeField] P002BattleMonsterConfig _config;
        [SerializeField] P002GameConfigBootstrap _gameConfigBootstrap;

        IP002BattleMonster _runtime;

        public IP002BattleMonster Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002BattleMonsterFactory.Create();
            if (_gameConfigBootstrap != null && _gameConfigBootstrap.Runtime != null)
            {
                _runtime.Init(_gameConfigBootstrap.Runtime);
            }
        }

        void Update()
        {
            if (_runtime != null)
                _runtime.Tick(Time.deltaTime);
        }
    }
}
