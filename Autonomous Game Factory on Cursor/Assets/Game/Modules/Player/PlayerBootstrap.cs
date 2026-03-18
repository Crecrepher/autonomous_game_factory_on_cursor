using UnityEngine;

namespace Game
{
    public class PlayerBootstrap : MonoBehaviour
    {
        public IPlayer Runtime => _runtime;

        [SerializeField] PlayerConfig _config;

        IPlayer _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = PlayerFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
