using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002SkillSystemConfig", menuName = "Game/Modules/P002SkillSystemConfig")]
    public class P002SkillSystemConfig : ScriptableObject
    {
        const int DEFAULT_SKILL_GAUGE_MAX = 50;
        const int DEFAULT_GAUGE_PER_PIECE = 10;
        const int DEFAULT_PENDING_QUEUE_SIZE = 3;
        const float DEFAULT_AUTO_SKILL_DELAY = 0.5f;

        [SerializeField] EP002SkillMode _skillMode = EP002SkillMode.Manual;
        [SerializeField] float _autoSkillDelay = DEFAULT_AUTO_SKILL_DELAY;
        [SerializeField] int _skillGaugeMax = DEFAULT_SKILL_GAUGE_MAX;
        [SerializeField] int _gaugePerPiece = DEFAULT_GAUGE_PER_PIECE;
        [SerializeField] int _pendingQueueSize = DEFAULT_PENDING_QUEUE_SIZE;

        public EP002SkillMode SkillMode => _skillMode;
        public float AutoSkillDelay => _autoSkillDelay;
        public int SkillGaugeMax => _skillGaugeMax;
        public int GaugePerPiece => _gaugePerPiece;
        public int PendingQueueSize => _pendingQueueSize;
    }
}
