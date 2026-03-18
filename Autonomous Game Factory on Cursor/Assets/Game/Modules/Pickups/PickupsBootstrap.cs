using UnityEngine;

namespace Game
{
    public class PickupsBootstrap : MonoBehaviour
    {
        public IPickups Runtime => _runtime;

        [SerializeField] PickupsConfig _config;

        IPickups _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = PickupsFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
