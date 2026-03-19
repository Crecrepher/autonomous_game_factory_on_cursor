using System;

namespace Game
{
    public interface IP002PuzzleView
    {
        void Init(int boardWidth, int boardHeight);
        void Tick(float deltaTime);
        void NotifyBlockChanged(int x, int y, int blockType, int specialType);
        void NotifyBlocksMatched(int[] matchedX, int[] matchedY, int matchCount);
        void NotifyBlocksDropped(int column, int fromRow, int toRow);
        void NotifyBlockSpawned(int x, int y, int blockType);
        void NotifySwapStarted(int fromX, int fromY, int toX, int toY);
        void NotifySwapCompleted(bool success);
        void NotifySpecialBlockActivated(int x, int y, int specialType);
        void NotifyAnimationComplete();
        void NotifyBlockClicked(int x, int y);
        void SetInteractable(bool interactable);
        bool IsAnimating { get; }
        int GetDisplayedBlockType(int x, int y);
        int GetDisplayedSpecialType(int x, int y);
        event Action OnAllAnimationsComplete;
        event Action<int, int> OnBlockClicked;
    }
}
