using UnityEngine;

namespace Game
{
    public class P002BattleCharacterBootstrap : MonoBehaviour
    {
        [SerializeField] P002BattleCharacterConfig _config;

        IP002BattleCharacter _runtime;

        public IP002BattleCharacter Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002BattleCharacterFactory.Create(_config);
        }
    }
}
