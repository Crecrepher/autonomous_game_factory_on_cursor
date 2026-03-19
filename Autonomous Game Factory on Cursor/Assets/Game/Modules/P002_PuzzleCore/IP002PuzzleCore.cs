using System;

namespace Game
{
    public interface IP002PuzzleCore
    {
        void Init(int boardWidth, int boardHeight, int blockTypeCount, bool enableBomb, bool enableColorClear);
        void Tick(float deltaTime);
        bool TrySwap(int fromX, int fromY, int toX, int toY);
        int GetBlockType(int x, int y);
        int GetSpecialType(int x, int y);
        bool IsProcessing { get; }
        int BoardWidth { get; }
        int BoardHeight { get; }
        bool FindFirstSwappableMatch(out int fromX, out int fromY, out int toX, out int toY);
        bool FindFirstSpecialBlock(out int x, out int y);
        void ShuffleBoard();

        event Action<P002MatchResult[]> OnMatchCompleted;
        event Action OnRefillCompleted;
        event Action OnNoMovesAvailable;
    }
}
