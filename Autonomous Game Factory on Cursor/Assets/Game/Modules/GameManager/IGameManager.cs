namespace Game
{
    public interface IGameManager
    {
        void Init();
        void Tick(float deltaTime);

        int CurrentPhase { get; }
        bool IsGameOver { get; }

        void StartGame();
        void AdvancePhase();
        void EndGame();

        event System.Action<int> OnPhaseChanged;
        event System.Action OnGameStarted;
        event System.Action OnGameEnded;
    }
}
