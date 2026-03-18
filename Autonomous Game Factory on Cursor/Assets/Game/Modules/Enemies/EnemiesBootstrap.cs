using UnityEngine;

namespace Game
{
    public class EnemiesBootstrap : MonoBehaviour
    {
        public IEnemies Runtime => _runtime;

        [SerializeField] EnemiesConfig _config;

        IEnemies _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = EnemiesFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
