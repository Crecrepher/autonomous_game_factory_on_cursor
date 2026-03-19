using System;

namespace Game
{
    public class P002PuzzleViewRuntime : IP002PuzzleView
    {
        const int EMPTY_BLOCK_TYPE = -1;
        const int EMPTY_SPECIAL_TYPE = 0;

        readonly P002PuzzleViewConfig _config;

        int _boardWidth;
        int _boardHeight;
        int _activeAnimationCount;
        bool _isInteractable;
        int[,] _displayedBlockTypes;
        int[,] _displayedSpecialTypes;
        int _swapFromX;
        int _swapFromY;
        int _swapToX;
        int _swapToY;
        int _swapFromBlockType;
        int _swapFromSpecialType;
        int _swapToBlockType;
        int _swapToSpecialType;
        bool _swapPendingCompletion;

        public event Action OnAllAnimationsComplete;
        public event Action<int, int> OnBlockClicked;

        public bool IsAnimating => _activeAnimationCount > 0;

        public P002PuzzleViewRuntime(P002PuzzleViewConfig config)
        {
            _config = config;
        }

        public void Init(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _activeAnimationCount = 0;
            _isInteractable = true;
            _swapPendingCompletion = false;
            _displayedBlockTypes = new int[boardWidth, boardHeight];
            _displayedSpecialTypes = new int[boardWidth, boardHeight];

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    _displayedBlockTypes[x, y] = EMPTY_BLOCK_TYPE;
                    _displayedSpecialTypes[x, y] = EMPTY_SPECIAL_TYPE;
                }
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void NotifyBlockChanged(int x, int y, int blockType, int specialType)
        {
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                return;
            _displayedBlockTypes[x, y] = blockType;
            _displayedSpecialTypes[x, y] = specialType;
        }

        public void NotifyBlocksMatched(int[] matchedX, int[] matchedY, int matchCount)
        {
            if (matchedX == null || matchedY == null)
                return;
            for (int i = 0; i < matchCount && i < matchedX.Length && i < matchedY.Length; i++)
            {
                int x = matchedX[i];
                int y = matchedY[i];
                if (x >= 0 && x < _boardWidth && y >= 0 && y < _boardHeight)
                {
                    _displayedBlockTypes[x, y] = EMPTY_BLOCK_TYPE;
                    _displayedSpecialTypes[x, y] = EMPTY_SPECIAL_TYPE;
                }
            }
            _activeAnimationCount++;
        }

        public void NotifyBlocksDropped(int column, int fromRow, int toRow)
        {
            if (column < 0 || column >= _boardWidth || fromRow < 0 || fromRow >= _boardHeight || toRow < 0 || toRow >= _boardHeight)
                return;
            _displayedBlockTypes[column, toRow] = _displayedBlockTypes[column, fromRow];
            _displayedSpecialTypes[column, toRow] = _displayedSpecialTypes[column, fromRow];
            _displayedBlockTypes[column, fromRow] = EMPTY_BLOCK_TYPE;
            _displayedSpecialTypes[column, fromRow] = EMPTY_SPECIAL_TYPE;
            if (fromRow != toRow)
            {
                _activeAnimationCount++;
            }
        }

        public void NotifyBlockSpawned(int x, int y, int blockType)
        {
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                return;
            _displayedBlockTypes[x, y] = blockType;
            _displayedSpecialTypes[x, y] = EMPTY_SPECIAL_TYPE;
            _activeAnimationCount++;
        }

        public void NotifySwapStarted(int fromX, int fromY, int toX, int toY)
        {
            if (fromX < 0 || fromX >= _boardWidth || fromY < 0 || fromY >= _boardHeight)
                return;
            if (toX < 0 || toX >= _boardWidth || toY < 0 || toY >= _boardHeight)
                return;

            _swapFromX = fromX;
            _swapFromY = fromY;
            _swapToX = toX;
            _swapToY = toY;
            _swapFromBlockType = _displayedBlockTypes[fromX, fromY];
            _swapFromSpecialType = _displayedSpecialTypes[fromX, fromY];
            _swapToBlockType = _displayedBlockTypes[toX, toY];
            _swapToSpecialType = _displayedSpecialTypes[toX, toY];
            _swapPendingCompletion = true;
            _activeAnimationCount++;
        }

        public void NotifySwapCompleted(bool success)
        {
            if (_swapPendingCompletion)
            {
                if (success)
                {
                    _displayedBlockTypes[_swapFromX, _swapFromY] = _swapToBlockType;
                    _displayedSpecialTypes[_swapFromX, _swapFromY] = _swapToSpecialType;
                    _displayedBlockTypes[_swapToX, _swapToY] = _swapFromBlockType;
                    _displayedSpecialTypes[_swapToX, _swapToY] = _swapFromSpecialType;
                }
                _swapPendingCompletion = false;
            }
            DecrementAnimationAndMaybeFireComplete();
        }

        public void NotifySpecialBlockActivated(int x, int y, int specialType)
        {
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                return;
            _displayedBlockTypes[x, y] = EMPTY_BLOCK_TYPE;
            _displayedSpecialTypes[x, y] = EMPTY_SPECIAL_TYPE;
            _activeAnimationCount++;
        }

        public void NotifyAnimationComplete()
        {
            DecrementAnimationAndMaybeFireComplete();
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        public void NotifyBlockClicked(int x, int y)
        {
            if (_isInteractable && OnBlockClicked != null)
            {
                OnBlockClicked(x, y);
            }
        }

        public int GetDisplayedBlockType(int x, int y)
        {
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                return EMPTY_BLOCK_TYPE;
            return _displayedBlockTypes[x, y];
        }

        public int GetDisplayedSpecialType(int x, int y)
        {
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                return EMPTY_SPECIAL_TYPE;
            return _displayedSpecialTypes[x, y];
        }

        void DecrementAnimationAndMaybeFireComplete()
        {
            if (_activeAnimationCount <= 0)
                return;
            bool wasAnimating = _activeAnimationCount > 0;
            _activeAnimationCount--;
            if (wasAnimating && _activeAnimationCount == 0)
            {
                if (OnAllAnimationsComplete != null)
                {
                    OnAllAnimationsComplete();
                }
            }
        }

    }
}
