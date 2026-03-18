using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class PlayerTests
    {
        PlayerConfig _config;
        IPlayer _runtime;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerConfig>();
            _runtime = PlayerFactory.CreateRuntime(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
            _runtime = null;
        }

        [Test]
        public void CreateRuntime_WithConfig_ReturnsNonNull()
        {
            Assert.IsNotNull(_runtime);
        }

        [Test]
        public void Init_ThenTick_DoesNotThrow()
        {
            _runtime.Init();
            Assert.DoesNotThrow(TickOnce);
        }

        [Test]
        public void Init_SetsConfigValues()
        {
            _runtime.Init();
            Assert.AreEqual(_config.MoveSpeed, _runtime.MoveSpeed);
            Assert.AreEqual(_config.AttackSpeed, _runtime.AttackSpeed);
            Assert.AreEqual(_config.AttackDamage, _runtime.AttackDamage);
            Assert.AreEqual(_config.AttackRange, _runtime.AttackRange);
        }

        [Test]
        public void SetMoveSpeed_UpdatesSpeed()
        {
            _runtime.Init();
            float newSpeed = 10f;
            _runtime.SetMoveSpeed(newSpeed);
            Assert.AreEqual(newSpeed, _runtime.MoveSpeed);
        }

        [Test]
        public void SetAttackSpeed_UpdatesSpeed()
        {
            _runtime.Init();
            float newSpeed = 3f;
            _runtime.SetAttackSpeed(newSpeed);
            Assert.AreEqual(newSpeed, _runtime.AttackSpeed);
        }

        [Test]
        public void SetMoveDirection_StoresValues()
        {
            _runtime.Init();
            _runtime.SetMoveDirection(1f, 0f);
            PlayerRuntime concrete = (PlayerRuntime)_runtime;
            Assert.AreEqual(1f, concrete.GetMoveDirX());
            Assert.AreEqual(0f, concrete.GetMoveDirY());
        }

        [Test]
        public void IsAttacking_DefaultFalse()
        {
            _runtime.Init();
            Assert.IsFalse(_runtime.IsAttacking);
        }

        void TickOnce()
        {
            _runtime.Tick(0.016f);
        }
    }
}
