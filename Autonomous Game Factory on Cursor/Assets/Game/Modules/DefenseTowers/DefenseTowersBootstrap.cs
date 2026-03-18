using UnityEngine;

namespace Game
{
    public class DefenseTowersBootstrap : MonoBehaviour
    {
        public IDefenseTowers Runtime => _runtime;

        [SerializeField] DefenseTowersConfig _config;

        IDefenseTowers _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = DefenseTowersFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
