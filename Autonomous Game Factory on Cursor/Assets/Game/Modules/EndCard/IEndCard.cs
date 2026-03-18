namespace Game
{
    public interface IEndCard
    {
        void Init();
        void Tick(float deltaTime);

        bool IsVisible { get; }

        void Show();
        void Hide();

        event System.Action OnCTAClicked;
        event System.Action OnShown;
    }
}
