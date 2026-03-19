using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002InputGuideConfig", menuName = "Game/Modules/P002InputGuideConfig")]
    public class P002InputGuideConfig : ScriptableObject
    {
        const float DEFAULT_IDLE_GUIDE_DELAY = 3f;
        const float DEFAULT_INACTIVITY_TIMEOUT = 30f;
        const float DEFAULT_FINGER_MOVE_SPEED = 3f;
        const float DEFAULT_FINGER_FADE_IN_DURATION = 0.5f;
        const float DEFAULT_FINGER_MOVE_DURATION = 1.0f;
        const float DEFAULT_FINGER_FADE_OUT_DURATION = 0.5f;
        const float DEFAULT_FINGER_PAUSE_DURATION = 0.5f;
        const int DEFAULT_FIXED_FROM_X = 4;
        const int DEFAULT_FIXED_FROM_Y = 2;
        const int DEFAULT_FIXED_TO_X = 3;
        const int DEFAULT_FIXED_TO_Y = 2;

        [SerializeField] float _idleGuideDelay = DEFAULT_IDLE_GUIDE_DELAY;
        [SerializeField] float _inactivityTimeout = DEFAULT_INACTIVITY_TIMEOUT;
        [SerializeField] float _fingerMoveSpeed = DEFAULT_FINGER_MOVE_SPEED;
        [SerializeField] float _fingerFadeInDuration = DEFAULT_FINGER_FADE_IN_DURATION;
        [SerializeField] float _fingerMoveDuration = DEFAULT_FINGER_MOVE_DURATION;
        [SerializeField] float _fingerFadeOutDuration = DEFAULT_FINGER_FADE_OUT_DURATION;
        [SerializeField] float _fingerPauseDuration = DEFAULT_FINGER_PAUSE_DURATION;
        [SerializeField] bool _useFixedHint = true;
        [SerializeField] int _fixedFromX = DEFAULT_FIXED_FROM_X;
        [SerializeField] int _fixedFromY = DEFAULT_FIXED_FROM_Y;
        [SerializeField] int _fixedToX = DEFAULT_FIXED_TO_X;
        [SerializeField] int _fixedToY = DEFAULT_FIXED_TO_Y;

        public float IdleGuideDelay => _idleGuideDelay;
        public float InactivityTimeout => _inactivityTimeout;
        public float FingerMoveSpeed => _fingerMoveSpeed;
        public float FingerFadeInDuration => _fingerFadeInDuration;
        public float FingerMoveDuration => _fingerMoveDuration;
        public float FingerFadeOutDuration => _fingerFadeOutDuration;
        public float FingerPauseDuration => _fingerPauseDuration;
        public bool UseFixedHint => _useFixedHint;
        public int FixedFromX => _fixedFromX;
        public int FixedFromY => _fixedFromY;
        public int FixedToX => _fixedToX;
        public int FixedToY => _fixedToY;
    }
}
