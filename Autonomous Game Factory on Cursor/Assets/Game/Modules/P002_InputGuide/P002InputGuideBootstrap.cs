using UnityEngine;

namespace Game
{
    public class P002InputGuideBootstrap : MonoBehaviour
    {
        [SerializeField] P002InputGuideConfig _config;
        [SerializeField] int _boardWidth = 6;
        [SerializeField] int _boardHeight = 8;

        IP002InputGuide _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002InputGuideFactory.Create(_config);
            if (_runtime != null)
            {
                _runtime.Init(_boardWidth, _boardHeight);
            }
        }
    }
}
