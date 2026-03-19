using System;

namespace Game
{
    public interface IP002GameFlow
    {
        event Action OnGameStarted;
        event Action<bool> OnGameEnded;
        event Action<int> OnStageChanged;

        void Init();
        void Tick(float deltaTime);
        void StartGame();
        void EndGame(bool isSuccess);

        int CurrentStage { get; }
        int TotalStages { get; }
        bool IsGameStarted { get; }
        bool IsGameEnded { get; }
    }
}
