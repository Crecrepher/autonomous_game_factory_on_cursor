using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "BlacksmithConfig", menuName = "Game/Modules/BlacksmithConfig")]
    public class BlacksmithConfig : ScriptableObject
    {
        [SerializeField] int _forgeCost = 50;
        [SerializeField] int _maxForgeCount = 4;
        [SerializeField] int _equipmentPerForge = 6;

        public int ForgeCost => _forgeCost;
        public int MaxForgeCount => _maxForgeCount;
        public int EquipmentPerForge => _equipmentPerForge;
    }
}
