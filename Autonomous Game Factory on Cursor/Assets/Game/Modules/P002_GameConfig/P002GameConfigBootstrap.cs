using UnityEngine;

namespace Game
{
    public class P002GameConfigBootstrap : MonoBehaviour
    {
        [SerializeField] P002GameConfigConfig _config;

        IP002GameConfig _runtime;

        public IP002GameConfig Runtime => _runtime;

        void Start()
        {
            if (_config == null) return;
            _runtime = P002GameConfigFactory.Create(_config);
        }
    }
}
