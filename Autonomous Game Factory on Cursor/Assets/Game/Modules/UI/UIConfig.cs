using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "Game/Modules/UIConfig")]
    public class UIConfig : ScriptableObject
    {
        [SerializeField] bool _showCoinOnStart = true;

        public bool ShowCoinOnStart => _showCoinOnStart;
    }
}
