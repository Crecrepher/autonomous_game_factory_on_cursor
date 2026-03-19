using UnityEngine;

namespace Game
{
    public class P002GameSoundBootstrap : MonoBehaviour
    {
        [SerializeField] P002GameSoundConfig _config;

        IP002GameSound _runtime;

        public IP002GameSound Runtime => _runtime;

        void Start()
        {
            if (_config == null) return;
            _runtime = P002GameSoundFactory.Create(_config);
            if (_runtime != null)
            {
                _runtime.Init();
                P002GameSoundRuntime concreteRuntime = _runtime as P002GameSoundRuntime;
                if (concreteRuntime != null)
                {
                    concreteRuntime.OnSoundRequested += HandleSoundRequested;
                }
            }
        }

        void OnDestroy()
        {
            if (_runtime != null)
            {
                _runtime.Release();
                P002GameSoundRuntime concreteRuntime = _runtime as P002GameSoundRuntime;
                if (concreteRuntime != null)
                {
                    concreteRuntime.OnSoundRequested -= HandleSoundRequested;
                }
            }
        }

        void HandleSoundRequested(int soundIndex)
        {
            if (_runtime == null) return;
            if (!_runtime.IsEnabled) return;
        }
    }
}
