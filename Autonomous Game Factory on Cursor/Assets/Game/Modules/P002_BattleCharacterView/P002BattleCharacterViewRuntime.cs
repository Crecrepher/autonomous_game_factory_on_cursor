using System;

namespace Game
{
    public class P002BattleCharacterViewRuntime : IP002BattleCharacterView
    {
        const int MAX_CHARACTER_COUNT = 10;

        public event Action<int> OnSkillButtonClicked;
        public event Action<int> OnAttackAnimationRequested;
        public event Action<int> OnSkillAnimationRequested;

        int _characterCount;
        float[] _gaugeRatios;
        bool[] _isSkillReady;
        bool[] _isInteractable;

        public void Init(int characterCount)
        {
            _characterCount = characterCount > MAX_CHARACTER_COUNT ? MAX_CHARACTER_COUNT : characterCount;
            _gaugeRatios = new float[_characterCount];
            _isSkillReady = new bool[_characterCount];
            _isInteractable = new bool[_characterCount];

            for (int i = 0; i < _characterCount; i++)
            {
                _gaugeRatios[i] = 0f;
                _isSkillReady[i] = false;
                _isInteractable[i] = true;
            }
        }

        public void SetGaugeRatio(int index, float ratio)
        {
            if (index < 0 || index >= _characterCount) return;
            _gaugeRatios[index] = ratio;
        }

        public void SetSkillReady(int index, bool ready)
        {
            if (index < 0 || index >= _characterCount) return;
            _isSkillReady[index] = ready;
        }

        public void PlayAttackAnimation(int index)
        {
            if (index < 0 || index >= _characterCount) return;
            if (OnAttackAnimationRequested != null) OnAttackAnimationRequested.Invoke(index);
        }

        public void PlaySkillAnimation(int index)
        {
            if (index < 0 || index >= _characterCount) return;
            if (OnSkillAnimationRequested != null) OnSkillAnimationRequested.Invoke(index);
        }

        public void PlayIdleAnimation(int index)
        {
            if (index < 0 || index >= _characterCount) return;
        }

        public void SetInteractable(int index, bool interactable)
        {
            if (index < 0 || index >= _characterCount) return;
            _isInteractable[index] = interactable;
        }

        public void NotifySkillButtonClicked(int index)
        {
            if (index < 0 || index >= _characterCount) return;
            if (OnSkillButtonClicked != null) OnSkillButtonClicked.Invoke(index);
        }

        public float GetGaugeRatio(int index)
        {
            if (index < 0 || index >= _characterCount) return 0f;
            return _gaugeRatios[index];
        }

        public bool GetSkillReady(int index)
        {
            if (index < 0 || index >= _characterCount) return false;
            return _isSkillReady[index];
        }
    }
}
