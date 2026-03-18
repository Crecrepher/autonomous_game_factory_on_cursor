using System;

namespace Game
{
    public class GameManagerRuntime : IGameManager
    {
        const int INITIAL_PHASE = 0;

        public event Action<int> OnPhaseChanged;
        public event Action OnGameStarted;
        public event Action OnGameEnded;

        public int CurrentPhase => _currentPhase;
        public bool IsGameOver => _isGameOver;

        readonly GameManagerConfig _config;
        int _currentPhase;
        bool _isGameOver;
        bool _isGameStarted;

        public GameManagerRuntime(GameManagerConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _currentPhase = INITIAL_PHASE;
            _isGameOver = false;
            _isGameStarted = false;
        }

        public void Tick(float deltaTime)
        {
        }

        public void StartGame()
        {
            if (_isGameStarted)
                return;

            _isGameStarted = true;
            _currentPhase = INITIAL_PHASE;

            if (OnGameStarted != null)
                OnGameStarted.Invoke();
        }

        public void AdvancePhase()
        {
            if (_isGameOver)
                return;

            if (_currentPhase >= _config.TotalPhases)
                return;

            _currentPhase++;

            if (OnPhaseChanged != null)
                OnPhaseChanged.Invoke(_currentPhase);
        }

        public void EndGame()
        {
            if (_isGameOver)
                return;

            _isGameOver = true;

            if (OnGameEnded != null)
                OnGameEnded.Invoke();
        }
    }
}
