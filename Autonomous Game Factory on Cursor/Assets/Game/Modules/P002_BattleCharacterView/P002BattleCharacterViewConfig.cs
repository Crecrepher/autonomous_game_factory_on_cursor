using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002BattleCharacterViewConfig", menuName = "Game/Modules/P002BattleCharacterViewConfig")]
    public class P002BattleCharacterViewConfig : ScriptableObject
    {
        const float DEFAULT_SKILL_SCALE_UP_DURATION = 0.15f;
        const float DEFAULT_SKILL_SCALE_DOWN_DURATION = 0.2f;
        const float DEFAULT_GAUGE_LERP_SPEED = 8f;
        const float DEFAULT_SKILL_SCALE_UP = 1.2f;

        [SerializeField] float _skillScaleUpDuration = DEFAULT_SKILL_SCALE_UP_DURATION;
        [SerializeField] float _skillScaleDownDuration = DEFAULT_SKILL_SCALE_DOWN_DURATION;
        [SerializeField] float _gaugeLerpSpeed = DEFAULT_GAUGE_LERP_SPEED;
        [SerializeField] float _skillScaleUp = DEFAULT_SKILL_SCALE_UP;

        public float SkillScaleUpDuration => _skillScaleUpDuration;
        public float SkillScaleDownDuration => _skillScaleDownDuration;
        public float GaugeLerpSpeed => _gaugeLerpSpeed;
        public float SkillScaleUp => _skillScaleUp;
    }
}
