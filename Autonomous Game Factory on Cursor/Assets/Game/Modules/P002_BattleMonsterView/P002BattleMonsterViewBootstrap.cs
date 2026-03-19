using UnityEngine;

namespace Game
{
    public class P002BattleMonsterViewBootstrap : MonoBehaviour
    {
        [SerializeField] P002BattleMonsterViewConfig _config;

        IP002BattleMonsterView _runtime;

        public IP002BattleMonsterView Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002BattleMonsterViewFactory.Create(_config);
        }
    }
}
