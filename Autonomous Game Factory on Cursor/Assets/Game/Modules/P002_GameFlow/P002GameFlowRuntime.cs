using System;

namespace Game
{
    public class P002GameFlowRuntime : IP002GameFlow
    {
        public event Action OnGameStarted;
        public event Action<bool> OnGameEnded;
        public event Action<int> OnStageChanged;

        EP002GameState _currentState;
        int _currentStage;
        int _totalStages;
        bool _isGameStarted;
        bool _isGameEnded;
        float _stageTransitionTimer;
        float _stageTransitionDelay;

        public int CurrentStage => _currentStage;
        public int TotalStages => _totalStages;
        public bool IsGameStarted => _isGameStarted;
        public bool IsGameEnded => _isGameEnded;
        public EP002GameState CurrentState => _currentState;

        public void Init()
        {
            _currentState = EP002GameState.Initializing;
            _currentStage = 0;
            _stageTransitionTimer = 0f;
        }

        public void Configure(int totalStages, float stageTransitionDelay)
        {
            _totalStages = totalStages;
            _stageTransitionDelay = stageTransitionDelay;
        }

        public void Tick(float deltaTime)
        {
            if (_currentState != EP002GameState.StageTransition)
                return;

            _stageTransitionTimer += deltaTime;
            if (_stageTransitionTimer >= _stageTransitionDelay)
            {
                AdvanceStage();
            }
        }

        public void StartGame()
        {
            if (_isGameStarted || _isGameEnded)
                return;

            _isGameStarted = true;
            _currentState = EP002GameState.Playing;

            if (OnGameStarted != null)
                OnGameStarted.Invoke();
        }

        public void EndGame(bool isSuccess)
        {
            if (_isGameEnded)
                return;

            _isGameEnded = true;
            _currentState = EP002GameState.Ended;

            if (OnGameEnded != null)
                OnGameEnded.Invoke(isSuccess);
        }

        public void AdvanceStage()
        {
            _currentStage++;
            _stageTransitionTimer = 0f;

            if (_currentStage >= _totalStages)
            {
                EndGame(true);
                return;
            }

            if (OnStageChanged != null)
                OnStageChanged.Invoke(_currentStage);

            _currentState = EP002GameState.Playing;
        }

        public void NotifyMonsterDefeated()
        {
            if (_currentState != EP002GameState.Playing && _currentState != EP002GameState.SkillAnimating)
                return;

            _currentState = EP002GameState.StageTransition;
            _stageTransitionTimer = 0f;
        }

        public void NotifySkillActivated()
        {
            if (_currentState == EP002GameState.Playing)
                _currentState = EP002GameState.SkillAnimating;
        }

        public void NotifySkillCompleted()
        {
            if (_currentState == EP002GameState.SkillAnimating)
                _currentState = EP002GameState.Playing;
        }
    }
}
