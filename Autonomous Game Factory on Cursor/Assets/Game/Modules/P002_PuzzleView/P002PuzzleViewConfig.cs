using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002PuzzleViewConfig", menuName = "Game/Modules/P002PuzzleViewConfig")]
    public class P002PuzzleViewConfig : ScriptableObject
    {
        const float DEFAULT_SWAP_DURATION = 0.2f;
        const float DEFAULT_DESTROY_DURATION = 0.15f;
        const float DEFAULT_DROP_DURATION = 0.1f;
        const float DEFAULT_SPAWN_DURATION = 0.15f;
        const float DEFAULT_BLOCK_SIZE = 1.0f;
        const float DEFAULT_BLOCK_SPACING = 0.1f;

        [SerializeField] float _swapDuration = DEFAULT_SWAP_DURATION;
        [SerializeField] float _destroyDuration = DEFAULT_DESTROY_DURATION;
        [SerializeField] float _dropDuration = DEFAULT_DROP_DURATION;
        [SerializeField] float _spawnDuration = DEFAULT_SPAWN_DURATION;
        [SerializeField] float _blockSize = DEFAULT_BLOCK_SIZE;
        [SerializeField] float _blockSpacing = DEFAULT_BLOCK_SPACING;

        public float SwapDuration => _swapDuration;
        public float DestroyDuration => _destroyDuration;
        public float DropDuration => _dropDuration;
        public float SpawnDuration => _spawnDuration;
        public float BlockSize => _blockSize;
        public float BlockSpacing => _blockSpacing;
    }
}
