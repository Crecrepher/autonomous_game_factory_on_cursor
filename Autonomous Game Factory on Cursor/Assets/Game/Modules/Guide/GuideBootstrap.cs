using UnityEngine;

namespace Game
{
    public class GuideBootstrap : MonoBehaviour
    {
        public IGuide Runtime => _runtime;

        [SerializeField] GuideConfig _config;

        IGuide _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = GuideFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
