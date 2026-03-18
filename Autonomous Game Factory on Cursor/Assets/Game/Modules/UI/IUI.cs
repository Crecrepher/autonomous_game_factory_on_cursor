namespace Game
{
    public interface IUI
    {
        void Init();
        void Tick(float deltaTime);

        bool IsVisible { get; }
        void Show();
        void Hide();
        void UpdateCoinDisplay(int amount);
        void ShowCTA();
        void HideCTA();

        event System.Action OnCTAClicked;
    }
}
