using UnityEngine;

namespace Game
{
    public class WarriorsBootstrap : MonoBehaviour
    {
        public IWarriors Runtime => _runtime;

        [SerializeField] WarriorsConfig _config;

        IWarriors _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = WarriorsFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
