using System;

namespace Game
{
    public interface IP002EndCard
    {
        void Init();
        void Show();
        void Hide();
        void NotifyCTAClicked();
        bool IsVisible { get; }
        event Action OnCTAClicked;
    }
}
