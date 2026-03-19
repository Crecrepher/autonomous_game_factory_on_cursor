using System;

namespace Game
{
    public interface IP002SkillSystem
    {
        void Init(IP002GameConfig config);
        void Tick(float deltaTime);
        bool TryActivateSkill(int characterIndex);
        void OnSkillAnimationComplete(int characterIndex);
        void SetBlocked(bool blocked);
        float GetGaugeRatio(int characterIndex);
        bool IsSkillReady(int characterIndex);
        void AddGauge(int characterIndex, int pieceCount);

        event Action<int> OnSkillReady;
        event Action<int> OnSkillExecuted;
    }
}
