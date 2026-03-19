using UnityEngine;

namespace Game
{
    public class P002GameEventsBootstrap : MonoBehaviour
    {
        [SerializeField] P002GameEventsConfig _config;

        IP002GameEvents _runtime;

        public IP002GameEvents Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002GameEventsFactory.Create(_config);
        }
    }
}
