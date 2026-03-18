using System;

namespace Game
{
    public class EndCardRuntime : IEndCard
    {
        public event Action OnCTAClicked;
        public event Action OnShown;

        public bool IsVisible => _isVisible;

        readonly EndCardConfig _config;
        bool _isVisible;

        public EndCardRuntime(EndCardConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _isVisible = false;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Show()
        {
            if (_isVisible)
                return;

            _isVisible = true;

            if (OnShown != null)
                OnShown.Invoke();
        }

        public void Hide()
        {
            _isVisible = false;
        }

        public void SimulateCTAClick()
        {
            if (OnCTAClicked != null)
                OnCTAClicked.Invoke();
        }

        public float GetDimAlpha()
        {
            return _config.DimAlpha;
        }

        public float GetFadeInDuration()
        {
            return _config.FadeInDuration;
        }
    }
}
