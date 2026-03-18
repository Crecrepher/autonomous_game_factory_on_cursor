using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameManagerConfig", menuName = "Game/Modules/GameManagerConfig")]
    public class GameManagerConfig : ScriptableObject
    {
        [SerializeField] int _totalPhases = 6;

        public int TotalPhases => _totalPhases;
    }
}
