using UnityEngine;

namespace Game
{
    public class P002BattleEffectBootstrap : MonoBehaviour
    {
        [SerializeField] P002BattleEffectConfig _config;

        IP002BattleEffect _runtime;

        public IP002BattleEffect Runtime => _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = P002BattleEffectFactory.Create(_config);
        }

        void Update()
        {
            if (_runtime != null)
                _runtime.Tick(Time.deltaTime);
        }
    }
}
