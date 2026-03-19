using System;

namespace Game
{
    public interface IP002BattleCharacterView
    {
        event Action<int> OnSkillButtonClicked;
        event Action<int> OnAttackAnimationRequested;
        event Action<int> OnSkillAnimationRequested;

        void Init(int characterCount);
        void SetGaugeRatio(int index, float ratio);
        void SetSkillReady(int index, bool ready);
        void PlayAttackAnimation(int index);
        void PlaySkillAnimation(int index);
        void PlayIdleAnimation(int index);
        void SetInteractable(int index, bool interactable);
    }
}
