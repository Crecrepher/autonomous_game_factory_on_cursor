using UnityEngine;

namespace Game
{
    public class P002PuzzleCoreBootstrap : MonoBehaviour
    {
        [SerializeField] P002PuzzleCoreConfig _config;

        IP002PuzzleCore _runtime;

        public IP002PuzzleCore Runtime => _runtime;

        void Awake()
        {
            if (_config == null) return;
            _runtime = P002PuzzleCoreFactory.Create(_config);
            if (_runtime != null)
            {
                _runtime.Init(
                    _config.BoardWidth,
                    _config.BoardHeight,
                    _config.BlockTypeCount,
                    _config.EnableBombBlock,
                    _config.EnableColorClearBlock);
            }
        }

        void Update()
        {
            if (_runtime != null)
            {
                _runtime.Tick(Time.deltaTime);
            }
        }
    }
}
