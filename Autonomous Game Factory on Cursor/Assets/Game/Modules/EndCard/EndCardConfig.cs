using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "EndCardConfig", menuName = "Game/Modules/EndCardConfig")]
    public class EndCardConfig : ScriptableObject
    {
        [SerializeField] float _dimAlpha = 0.7f;
        [SerializeField] float _fadeInDuration = 0.5f;

        public float DimAlpha => _dimAlpha;
        public float FadeInDuration => _fadeInDuration;
    }
}
