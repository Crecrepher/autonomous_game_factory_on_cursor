using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002EndCardConfig", menuName = "Game/Modules/P002EndCardConfig")]
    public class P002EndCardConfig : ScriptableObject
    {
        const float DEFAULT_FADE_IN_DURATION = 0.3f;
        const float DEFAULT_DIM_ALPHA = 0.7f;

        [SerializeField] float _fadeInDuration = DEFAULT_FADE_IN_DURATION;
        [SerializeField] float _dimAlpha = DEFAULT_DIM_ALPHA;

        public float FadeInDuration => _fadeInDuration;
        public float DimAlpha => _dimAlpha;
    }
}
