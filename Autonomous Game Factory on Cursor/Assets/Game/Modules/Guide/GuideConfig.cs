using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GuideConfig", menuName = "Game/Modules/GuideConfig")]
    public class GuideConfig : ScriptableObject
    {
        [SerializeField] int _maxActiveGuides = 3;

        public int MaxActiveGuides => _maxActiveGuides;
    }
}
