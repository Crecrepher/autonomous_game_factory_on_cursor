using System;

namespace Game
{
    public class GuideRuntime : IGuide
    {
        const int INVALID_ID = -1;

        public event Action<int> OnGuideShown;
        public event Action<int> OnGuideHidden;

        readonly GuideConfig _config;
        int[] _activeGuideIds;
        int _activeCount;

        public GuideRuntime(GuideConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _activeGuideIds = new int[_config.MaxActiveGuides];
            _activeCount = 0;

            for (int i = 0; i < _activeGuideIds.Length; i++)
            {
                _activeGuideIds[i] = INVALID_ID;
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void ShowGuide(int guideId)
        {
            if (IsGuideActive(guideId))
                return;

            if (_activeCount >= _config.MaxActiveGuides)
                return;

            for (int i = 0; i < _activeGuideIds.Length; i++)
            {
                if (_activeGuideIds[i] == INVALID_ID)
                {
                    _activeGuideIds[i] = guideId;
                    _activeCount++;

                    if (OnGuideShown != null)
                        OnGuideShown.Invoke(guideId);

                    return;
                }
            }
        }

        public void HideGuide(int guideId)
        {
            for (int i = 0; i < _activeGuideIds.Length; i++)
            {
                if (_activeGuideIds[i] == guideId)
                {
                    _activeGuideIds[i] = INVALID_ID;
                    _activeCount--;

                    if (OnGuideHidden != null)
                        OnGuideHidden.Invoke(guideId);

                    return;
                }
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < _activeGuideIds.Length; i++)
            {
                if (_activeGuideIds[i] != INVALID_ID)
                {
                    int id = _activeGuideIds[i];
                    _activeGuideIds[i] = INVALID_ID;

                    if (OnGuideHidden != null)
                        OnGuideHidden.Invoke(id);
                }
            }
            _activeCount = 0;
        }

        public bool IsGuideActive(int guideId)
        {
            for (int i = 0; i < _activeGuideIds.Length; i++)
            {
                if (_activeGuideIds[i] == guideId)
                    return true;
            }
            return false;
        }
    }
}
