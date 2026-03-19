using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "P002PuzzleCoreConfig", menuName = "Game/Modules/P002PuzzleCoreConfig")]
    public class P002PuzzleCoreConfig : ScriptableObject
    {
        public const int DEFAULT_WIDTH = 6;
        public const int DEFAULT_HEIGHT = 5;
        public const int DEFAULT_BLOCK_TYPE_COUNT = 3;
        public const int BOMB_MATCH_THRESHOLD = 4;
        public const int COLOR_CLEAR_MATCH_THRESHOLD = 5;
        public const int BOMB_RADIUS = 1;

        [SerializeField] int _boardWidth = DEFAULT_WIDTH;
        [SerializeField] int _boardHeight = DEFAULT_HEIGHT;
        [SerializeField] int _blockTypeCount = DEFAULT_BLOCK_TYPE_COUNT;
        [SerializeField] bool _enableBombBlock = true;
        [SerializeField] bool _enableColorClearBlock = true;
        [SerializeField] int _bombMatchThreshold = BOMB_MATCH_THRESHOLD;
        [SerializeField] int _colorClearMatchThreshold = COLOR_CLEAR_MATCH_THRESHOLD;

        public int BoardWidth => _boardWidth;
        public int BoardHeight => _boardHeight;
        public int BlockTypeCount => _blockTypeCount;
        public bool EnableBombBlock => _enableBombBlock;
        public bool EnableColorClearBlock => _enableColorClearBlock;
        public int BombMatchThreshold => _bombMatchThreshold;
        public int ColorClearMatchThreshold => _colorClearMatchThreshold;
    }
}
