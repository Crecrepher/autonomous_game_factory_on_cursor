using System;

namespace Game
{
    public class P002InputGuideRuntime : IP002InputGuide
    {
        readonly P002InputGuideConfig _config;

        int _boardWidth;
        int _boardHeight;
        float _idleTimer;
        bool _isGuideVisible;
        bool _isSkillGuideVisible;
        bool _isSpecialBlockGuideVisible;
        bool _isFirstInteractionDone;
        bool _isEnabled;
        int _guideFromX;
        int _guideFromY;
        int _guideToX;
        int _guideToY;

        public event Action<int, int, int, int> OnGuideShow;
        public event Action OnGuideHide;
        public event Action<int> OnSkillGuideShow;
        public event Action OnSkillGuideHide;

        public bool IsGuideVisible => _isGuideVisible;
        public bool IsSkillGuideVisible => _isSkillGuideVisible;
        public bool IsSpecialBlockGuideVisible => _isSpecialBlockGuideVisible;

        public P002InputGuideRuntime(P002InputGuideConfig config)
        {
            _config = config;
        }

        public void Init(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _idleTimer = 0f;
            _isGuideVisible = false;
            _isSkillGuideVisible = false;
            _isSpecialBlockGuideVisible = false;
            _isFirstInteractionDone = false;
            _isEnabled = true;
            _guideFromX = 0;
            _guideFromY = 0;
            _guideToX = 0;
            _guideToY = 0;
        }

        public void Tick(float deltaTime)
        {
            if (!_isEnabled || _isFirstInteractionDone)
                return;

            _idleTimer += deltaTime;

            if (_idleTimer >= _config.IdleGuideDelay && !_isGuideVisible)
            {
                if (_config.UseFixedHint)
                {
                    ShowGuide(_config.FixedFromX, _config.FixedFromY, _config.FixedToX, _config.FixedToY);
                }
                else
                {
                    int fromX = 0;
                    int fromY = 0;
                    int toX = 0;
                    int toY = 0;
                    if (_boardWidth > 1 && _boardHeight > 0)
                    {
                        fromX = _boardWidth - 1;
                        fromY = _boardHeight / 2;
                        toX = fromX - 1;
                        toY = fromY;
                    }
                    ShowGuide(fromX, fromY, toX, toY);
                }
            }
        }

        public void ShowGuide(int fromX, int fromY, int toX, int toY)
        {
            HideGuide();
            _guideFromX = fromX;
            _guideFromY = fromY;
            _guideToX = toX;
            _guideToY = toY;
            _isGuideVisible = true;
            if (OnGuideShow != null)
            {
                OnGuideShow(fromX, fromY, toX, toY);
            }
        }

        public void HideGuide()
        {
            if (_isGuideVisible)
            {
                _isGuideVisible = false;
                if (OnGuideHide != null)
                {
                    OnGuideHide();
                }
            }
        }

        public void ShowSkillGuide(int characterIndex)
        {
            HideSkillGuide();
            _isSkillGuideVisible = true;
            if (OnSkillGuideShow != null)
            {
                OnSkillGuideShow(characterIndex);
            }
        }

        public void HideSkillGuide()
        {
            if (_isSkillGuideVisible)
            {
                _isSkillGuideVisible = false;
                if (OnSkillGuideHide != null)
                {
                    OnSkillGuideHide();
                }
            }
        }

        public void ShowSpecialBlockGuide(int x, int y)
        {
            HideSpecialBlockGuide();
            _isSpecialBlockGuideVisible = true;
        }

        public void HideSpecialBlockGuide()
        {
            _isSpecialBlockGuideVisible = false;
        }

        public void NotifyInputDetected()
        {
            _idleTimer = 0f;
            HideGuide();
        }

        public void NotifyFirstInteraction()
        {
            _isFirstInteractionDone = true;
            HideGuide();
            HideSkillGuide();
            HideSpecialBlockGuide();
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                HideGuide();
                HideSkillGuide();
                HideSpecialBlockGuide();
            }
        }
    }
}
