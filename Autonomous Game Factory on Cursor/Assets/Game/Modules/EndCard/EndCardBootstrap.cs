using UnityEngine;

namespace Game
{
    public class EndCardBootstrap : MonoBehaviour
    {
        public IEndCard Runtime => _runtime;

        [SerializeField] EndCardConfig _config;

        IEndCard _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = EndCardFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
