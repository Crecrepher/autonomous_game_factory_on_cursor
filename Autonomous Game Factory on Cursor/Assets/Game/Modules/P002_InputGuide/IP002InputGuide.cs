using System;

namespace Game
{
    public interface IP002InputGuide
    {
        void Init(int boardWidth, int boardHeight);
        void Tick(float deltaTime);
        void ShowGuide(int fromX, int fromY, int toX, int toY);
        void HideGuide();
        void ShowSkillGuide(int characterIndex);
        void HideSkillGuide();
        void ShowSpecialBlockGuide(int x, int y);
        void HideSpecialBlockGuide();
        void NotifyInputDetected();
        void NotifyFirstInteraction();
        void SetEnabled(bool enabled);
        bool IsGuideVisible { get; }
        bool IsSkillGuideVisible { get; }
        event Action<int, int, int, int> OnGuideShow;
        event Action OnGuideHide;
        event Action<int> OnSkillGuideShow;
        event Action OnSkillGuideHide;
    }
}
