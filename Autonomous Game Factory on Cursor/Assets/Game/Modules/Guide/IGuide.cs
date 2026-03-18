namespace Game
{
    public interface IGuide
    {
        void Init();
        void Tick(float deltaTime);

        void ShowGuide(int guideId);
        void HideGuide(int guideId);
        void HideAll();
        bool IsGuideActive(int guideId);

        event System.Action<int> OnGuideShown;
        event System.Action<int> OnGuideHidden;
    }
}
