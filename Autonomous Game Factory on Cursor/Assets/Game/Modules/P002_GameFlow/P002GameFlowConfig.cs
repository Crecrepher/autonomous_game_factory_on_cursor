using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002GameFlowConfig", menuName = "Game/Modules/P002GameFlowConfig")]
    public class P002GameFlowConfig : ScriptableObject
    {
        [SerializeField] int _totalStages = 2;
        [SerializeField] float _stageTransitionDelay = 0.5f;
        [SerializeField] bool _autoStartGame = true;

        public int TotalStages => _totalStages;
        public float StageTransitionDelay => _stageTransitionDelay;
        public bool AutoStartGame => _autoStartGame;
    }
}
