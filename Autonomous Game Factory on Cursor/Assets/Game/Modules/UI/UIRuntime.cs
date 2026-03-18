using System;

namespace Game
{
    public class UIRuntime : IUI
    {
        public event Action OnCTAClicked;

        public bool IsVisible => _isVisible;

        readonly UIConfig _config;
        bool _isVisible;
        int _displayedCoinAmount;
        bool _ctaVisible;

        public UIRuntime(UIConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _isVisible = _config.ShowCoinOnStart;
            _displayedCoinAmount = 0;
            _ctaVisible = false;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Show()
        {
            _isVisible = true;
        }

        public void Hide()
        {
            _isVisible = false;
        }

        public void UpdateCoinDisplay(int amount)
        {
            _displayedCoinAmount = amount;
        }

        public void ShowCTA()
        {
            _ctaVisible = true;
        }

        public void HideCTA()
        {
            _ctaVisible = false;
        }

        public bool IsCTAVisible()
        {
            return _ctaVisible;
        }

        public int GetDisplayedCoinAmount()
        {
            return _displayedCoinAmount;
        }

        public void SimulateCTAClick()
        {
            if (OnCTAClicked != null)
                OnCTAClicked.Invoke();
        }
    }
}
