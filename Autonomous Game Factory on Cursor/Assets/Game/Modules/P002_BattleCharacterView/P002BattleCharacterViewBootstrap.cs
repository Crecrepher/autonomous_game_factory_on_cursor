using UnityEngine;

namespace Game
{
    public class P002BattleCharacterViewBootstrap : MonoBehaviour
    {
        [SerializeField] P002BattleCharacterViewConfig _config;

        IP002BattleCharacterView _runtime;

        public IP002BattleCharacterView Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002BattleCharacterViewFactory.Create(_config);
        }
    }
}
