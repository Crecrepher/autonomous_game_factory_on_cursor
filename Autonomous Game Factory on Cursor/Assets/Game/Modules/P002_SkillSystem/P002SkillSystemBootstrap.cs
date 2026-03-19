using UnityEngine;

namespace Game
{
    public class P002SkillSystemBootstrap : MonoBehaviour
    {
        [SerializeField] P002SkillSystemConfig _config;
        [SerializeField] P002GameConfigBootstrap _gameConfigBootstrap;

        IP002SkillSystem _runtime;

        public IP002SkillSystem Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002SkillSystemFactory.Create(_config);
            IP002GameConfig gameConfig = _gameConfigBootstrap != null ? _gameConfigBootstrap.Runtime : null;
            _runtime.Init(gameConfig);
        }

        void Update()
        {
            if (_runtime != null)
                _runtime.Tick(Time.deltaTime);
        }
    }
}
