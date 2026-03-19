using UnityEngine;

namespace Game
{
    public class P002PuzzleViewBootstrap : MonoBehaviour
    {
        [SerializeField] P002PuzzleViewConfig _config;
        [SerializeField] int _boardWidth = 6;
        [SerializeField] int _boardHeight = 8;

        IP002PuzzleView _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002PuzzleViewFactory.Create(_config);
            if (_runtime != null)
            {
                _runtime.Init(_boardWidth, _boardHeight);
            }
        }
    }
}
