using UnityEngine;

namespace Game
{
    public class P002GameFlowBootstrap : MonoBehaviour
    {
        [SerializeField] P002GameFlowConfig _config;

        IP002GameFlow _runtime;

        public IP002GameFlow Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002GameFlowFactory.Create(_config);

            if (_config.AutoStartGame)
                _runtime.StartGame();
        }

        void Update()
        {
            if (_runtime != null)
                _runtime.Tick(Time.deltaTime);
        }
    }
}
