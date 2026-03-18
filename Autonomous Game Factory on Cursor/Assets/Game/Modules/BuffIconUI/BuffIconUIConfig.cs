using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "BuffIconUIConfig", menuName = "Game/Modules/BuffIconUIConfig")]
    public class BuffIconUIConfig : ScriptableObject
    {
        [SerializeField] int _maxIcons = 8;

        public int MaxIcons => _maxIcons;
    }
}
