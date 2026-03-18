using UnityEngine;

namespace Game
{
    public class GameManagerBootstrap : MonoBehaviour
    {
        public IGameManager Runtime => _runtime;

        [SerializeField] GameManagerConfig _config;

        IGameManager _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = GameManagerFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
