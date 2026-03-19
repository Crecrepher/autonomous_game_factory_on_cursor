using UnityEngine;

namespace Game
{
    public class P002EndCardBootstrap : MonoBehaviour
    {
        [SerializeField] P002EndCardConfig _config;

        IP002EndCard _runtime;

        public IP002EndCard Runtime => _runtime;

        void Start()
        {
            if (_config == null) return;
            _runtime = P002EndCardFactory.Create(_config);
            if (_runtime != null)
            {
                _runtime.Init();
            }
        }
    }
}
