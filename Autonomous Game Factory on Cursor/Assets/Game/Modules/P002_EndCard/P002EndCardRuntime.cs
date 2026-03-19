using System;

namespace Game
{
    public class P002EndCardRuntime : IP002EndCard
    {
        readonly P002EndCardConfig _config;

        bool _isVisible;

        public bool IsVisible => _isVisible;

        public event Action OnCTAClicked;

        public P002EndCardRuntime(P002EndCardConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _isVisible = false;
        }

        public void Show()
        {
            _isVisible = true;
        }

        public void Hide()
        {
            _isVisible = false;
        }

        public void NotifyCTAClicked()
        {
            if (OnCTAClicked != null)
            {
                OnCTAClicked();
            }
        }
    }
}
