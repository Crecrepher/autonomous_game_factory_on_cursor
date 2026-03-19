using System;
using UnityEngine;

namespace Game
{
    public class P002PuzzleCoreRuntime : IP002PuzzleCore
    {
        const int MANHATTAN_ADJACENT = 1;
        const int MIN_MATCH_LENGTH = 3;
        const int SQUARE_SIZE = 2;
        const int MIN_BLOCK_TYPE = 1;
        const int NO_SPAWN_X = -1;
        const int NO_SPAWN_Y = -1;
        const int MAX_INIT_RETRIES = 20;
        const int BOMB_RADIUS = 1;

        public event Action<P002MatchResult[]> OnMatchCompleted;
        public event Action OnRefillCompleted;
        public event Action OnNoMovesAvailable;

        readonly P002PuzzleCoreConfig _config;
        int[,] _boardTypes;
        int[,] _boardSpecials;
        bool[,] _boardMatched;
        int _width;
        int _height;
        int _blockTypeCount;
        bool _enableBomb;
        bool _enableColorClear;
        int _bombThreshold;
        int _colorClearThreshold;
        bool _isProcessing;

        readonly int[] _tempMatchedX;
        readonly int[] _tempMatchedY;
        readonly int[] _tempMatchedGroup;
        readonly int[] _tempGroupCount;
        readonly int[] _tempGroupType;

        public bool IsProcessing => _isProcessing;
        public int BoardWidth => _width;
        public int BoardHeight => _height;

        public P002PuzzleCoreRuntime(P002PuzzleCoreConfig config)
        {
            _config = config;
            const int maxCells = 64;
            const int maxGroups = 16;
            _tempMatchedX = new int[maxCells];
            _tempMatchedY = new int[maxCells];
            _tempMatchedGroup = new int[maxCells];
            _tempGroupCount = new int[maxGroups];
            _tempGroupType = new int[maxGroups];
        }

        public void Init(int boardWidth, int boardHeight, int blockTypeCount, bool enableBomb, bool enableColorClear)
        {
            _width = boardWidth;
            _height = boardHeight;
            _blockTypeCount = blockTypeCount;
            _enableBomb = enableBomb;
            _enableColorClear = enableColorClear;
            _bombThreshold = _config != null ? _config.BombMatchThreshold : P002PuzzleCoreConfig.BOMB_MATCH_THRESHOLD;
            _colorClearThreshold = _config != null ? _config.ColorClearMatchThreshold : P002PuzzleCoreConfig.COLOR_CLEAR_MATCH_THRESHOLD;

            _boardTypes = new int[_width, _height];
            _boardSpecials = new int[_width, _height];
            _boardMatched = new bool[_width, _height];

            FillBoardNoInitialMatches();
        }

        public void Tick(float deltaTime)
        {
        }

        public bool TrySwap(int fromX, int fromY, int toX, int toY)
        {
            if (_isProcessing) return false;
            if (!IsAdjacent(fromX, fromY, toX, toY)) return false;
            if (!IsInBounds(fromX, fromY) || !IsInBounds(toX, toY)) return false;

            SwapCells(fromX, fromY, toX, toY);

            if (!HasAnyMatch())
            {
                SwapCells(fromX, fromY, toX, toY);
                return false;
            }

            ProcessMatches(toX, toY);
            return true;
        }

        public int GetBlockType(int x, int y)
        {
            if (!IsInBounds(x, y)) return 0;
            return _boardTypes[x, y];
        }

        public int GetSpecialType(int x, int y)
        {
            if (!IsInBounds(x, y)) return 0;
            return _boardSpecials[x, y];
        }

        public bool FindFirstSwappableMatch(out int fromX, out int fromY, out int toX, out int toY)
        {
            fromX = 0;
            fromY = 0;
            toX = 0;
            toY = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int nx = x + 1;
                    if (nx < _width && WouldSwapCreateMatch(x, y, nx, y))
                    {
                        fromX = x;
                        fromY = y;
                        toX = nx;
                        toY = y;
                        return true;
                    }

                    int ny = y + 1;
                    if (ny < _height && WouldSwapCreateMatch(x, y, x, ny))
                    {
                        fromX = x;
                        fromY = y;
                        toX = x;
                        toY = ny;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool FindFirstSpecialBlock(out int x, out int y)
        {
            x = 0;
            y = 0;

            for (int ix = 0; ix < _width; ix++)
            {
                for (int iy = 0; iy < _height; iy++)
                {
                    if (_boardSpecials[ix, iy] != 0)
                    {
                        x = ix;
                        y = iy;
                        return true;
                    }
                }
            }

            return false;
        }

        public void ShuffleBoard()
        {
            FillBoardNoInitialMatches();
        }

        void FillBoardNoInitialMatches()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _boardTypes[x, y] = 0;
                    _boardSpecials[x, y] = 0;
                    int t = PickRandomTypeNoMatch(x, y);
                    _boardTypes[x, y] = t;
                }
            }
        }

        int PickRandomTypeNoMatch(int x, int y)
        {
            for (int retry = 0; retry < MAX_INIT_RETRIES; retry++)
            {
                int t = UnityEngine.Random.Range(MIN_BLOCK_TYPE, _blockTypeCount + 1);
                _boardTypes[x, y] = t;

                bool createsMatch = WouldCreateMatchAt(x, y);
                if (!createsMatch)
                {
                    return t;
                }
            }

            return UnityEngine.Random.Range(MIN_BLOCK_TYPE, _blockTypeCount + 1);
        }

        void ProcessMatches(int swapX, int swapY)
        {
            _isProcessing = true;

            int iterSwapX = swapX;
            int iterSwapY = swapY;

            while (true)
            {
                ClearMatchedFlags();
                FindAndMarkAllMatches();

                int matchedCount = CountMatchedCells();
                if (matchedCount == 0)
                {
                    break;
                }

                ExpandMatchesForSpecialActivation(iterSwapX, iterSwapY);

                P002MatchResult[] results = BuildMatchResults(iterSwapX, iterSwapY);
                if (OnMatchCompleted != null && results != null && results.Length > 0)
                {
                    OnMatchCompleted.Invoke(results);
                }

                ClearMatchedCells();
                SpawnSpecialsFromResults(results);
                ApplyGravity();
                RefillEmptyCells();

                if (OnRefillCompleted != null)
                {
                    OnRefillCompleted.Invoke();
                }

                iterSwapX = NO_SPAWN_X;
                iterSwapY = NO_SPAWN_Y;
            }

            _isProcessing = false;

            if (!HasValidMoves() && OnNoMovesAvailable != null)
            {
                OnNoMovesAvailable.Invoke();
            }
        }

        bool IsAdjacent(int fromX, int fromY, int toX, int toY)
        {
            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            return dx + dy == MANHATTAN_ADJACENT;
        }

        bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        void SwapCells(int ax, int ay, int bx, int by)
        {
            int tType = _boardTypes[ax, ay];
            int tSpec = _boardSpecials[ax, ay];

            _boardTypes[ax, ay] = _boardTypes[bx, by];
            _boardSpecials[ax, ay] = _boardSpecials[bx, by];

            _boardTypes[bx, by] = tType;
            _boardSpecials[bx, by] = tSpec;
        }

        bool HasAnyMatch()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (WouldCreateMatchAt(x, y))
                    {
                        return true;
                    }
                }
            }

            if (_width >= SQUARE_SIZE && _height >= SQUARE_SIZE)
            {
                for (int x = 0; x <= _width - SQUARE_SIZE; x++)
                {
                    for (int y = 0; y <= _height - SQUARE_SIZE; y++)
                    {
                        if (HasSquareMatchAt(x, y))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool WouldSwapCreateMatch(int fromX, int fromY, int toX, int toY)
        {
            SwapCells(fromX, fromY, toX, toY);
            bool hasMatch = HasAnyMatch();
            SwapCells(fromX, fromY, toX, toY);
            return hasMatch;
        }

        bool WouldCreateMatchAt(int x, int y)
        {
            int t = _boardTypes[x, y];
            if (t == 0) return false;

            int hRun = 1;
            int ix = x - 1;
            while (ix >= 0 && _boardTypes[ix, y] == t)
            {
                hRun++;
                ix--;
            }
            ix = x + 1;
            while (ix < _width && _boardTypes[ix, y] == t)
            {
                hRun++;
                ix++;
            }
            if (hRun >= MIN_MATCH_LENGTH) return true;

            int vRun = 1;
            int iy = y - 1;
            while (iy >= 0 && _boardTypes[x, iy] == t)
            {
                vRun++;
                iy--;
            }
            iy = y + 1;
            while (iy < _height && _boardTypes[x, iy] == t)
            {
                vRun++;
                iy++;
            }
            if (vRun >= MIN_MATCH_LENGTH) return true;

            return false;
        }

        bool HasSquareMatchAt(int cx, int cy)
        {
            int t = _boardTypes[cx, cy];
            if (t == 0) return false;
            return _boardTypes[cx + 1, cy] == t &&
                   _boardTypes[cx, cy + 1] == t &&
                   _boardTypes[cx + 1, cy + 1] == t;
        }

        void ClearMatchedFlags()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _boardMatched[x, y] = false;
                }
            }
        }

        void FindAndMarkAllMatches()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int t = _boardTypes[x, y];
                    if (t == 0) continue;

                    int hRun = 1;
                    int ix = x - 1;
                    while (ix >= 0 && _boardTypes[ix, y] == t)
                    {
                        hRun++;
                        ix--;
                    }
                    ix = x + 1;
                    while (ix < _width && _boardTypes[ix, y] == t)
                    {
                        hRun++;
                        ix++;
                    }
                    if (hRun >= MIN_MATCH_LENGTH)
                    {
                        ix = x - 1;
                        while (ix >= 0 && _boardTypes[ix, y] == t)
                        {
                            _boardMatched[ix, y] = true;
                            ix--;
                        }
                        _boardMatched[x, y] = true;
                        ix = x + 1;
                        while (ix < _width && _boardTypes[ix, y] == t)
                        {
                            _boardMatched[ix, y] = true;
                            ix++;
                        }
                    }

                    int vRun = 1;
                    int iy = y - 1;
                    while (iy >= 0 && _boardTypes[x, iy] == t)
                    {
                        vRun++;
                        iy--;
                    }
                    iy = y + 1;
                    while (iy < _height && _boardTypes[x, iy] == t)
                    {
                        vRun++;
                        iy++;
                    }
                    if (vRun >= MIN_MATCH_LENGTH)
                    {
                        iy = y - 1;
                        while (iy >= 0 && _boardTypes[x, iy] == t)
                        {
                            _boardMatched[x, iy] = true;
                            iy--;
                        }
                        _boardMatched[x, y] = true;
                        iy = y + 1;
                        while (iy < _height && _boardTypes[x, iy] == t)
                        {
                            _boardMatched[x, iy] = true;
                            iy++;
                        }
                    }
                }
            }

            if (_width >= SQUARE_SIZE && _height >= SQUARE_SIZE)
            {
                for (int x = 0; x <= _width - SQUARE_SIZE; x++)
                {
                    for (int y = 0; y <= _height - SQUARE_SIZE; y++)
                    {
                        if (HasSquareMatchAt(x, y))
                        {
                            _boardMatched[x, y] = true;
                            _boardMatched[x + 1, y] = true;
                            _boardMatched[x, y + 1] = true;
                            _boardMatched[x + 1, y + 1] = true;
                        }
                    }
                }
            }
        }

        void ExpandMatchesForSpecialActivation(int swapX, int swapY)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!_boardMatched[x, y]) continue;

                    int spec = _boardSpecials[x, y];
                    if (spec == 0) continue;

                    if (spec == (int)EP002SpecialBlockType.Bomb && _enableBomb)
                    {
                        int r = BOMB_RADIUS;
                        for (int dx = -r; dx <= r; dx++)
                        {
                            for (int dy = -r; dy <= r; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (IsInBounds(nx, ny))
                                {
                                    _boardMatched[nx, ny] = true;
                                }
                            }
                        }
                    }
                    else if (spec == (int)EP002SpecialBlockType.ColorClear && _enableColorClear)
                    {
                        int clearType = _boardTypes[x, y];
                        if (clearType == 0)
                        {
                            for (int i = 0; i < _width; i++)
                            {
                                for (int j = 0; j < _height; j++)
                                {
                                    if (_boardTypes[i, j] != 0)
                                    {
                                        clearType = _boardTypes[i, j];
                                        break;
                                    }
                                }
                                if (clearType != 0) break;
                            }
                        }
                        if (clearType != 0)
                        {
                            for (int i = 0; i < _width; i++)
                            {
                                for (int j = 0; j < _height; j++)
                                {
                                    if (_boardTypes[i, j] == clearType)
                                    {
                                        _boardMatched[i, j] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        int CountMatchedCells()
        {
            int count = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_boardMatched[x, y]) count++;
                }
            }
            return count;
        }

        P002MatchResult[] BuildMatchResults(int swapX, int swapY)
        {
            for (int i = 0; i < _tempGroupCount.Length; i++)
            {
                _tempGroupCount[i] = 0;
                _tempGroupType[i] = 0;
            }

            int groupCount = 0;
            int writeIdx = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!_boardMatched[x, y]) continue;

                    int t = _boardTypes[x, y];
                    int g = -1;
                    for (int i = 0; i < groupCount; i++)
                    {
                        if (_tempGroupType[i] == t)
                        {
                            g = i;
                            break;
                        }
                    }
                    if (g < 0)
                    {
                        g = groupCount;
                        _tempGroupType[g] = t;
                        _tempGroupCount[g] = 0;
                        groupCount++;
                    }

                    _tempMatchedX[writeIdx] = x;
                    _tempMatchedY[writeIdx] = y;
                    _tempMatchedGroup[writeIdx] = g;
                    writeIdx++;
                    _tempGroupCount[g]++;
                }
            }

            if (groupCount == 0) return null;

            P002MatchResult[] results = new P002MatchResult[groupCount];
            int spawnX = swapX >= 0 ? swapX : 0;
            int spawnY = swapY >= 0 ? swapY : 0;
            int maxMatchCount = 0;
            int maxMatchIdx = 0;

            for (int g = 0; g < groupCount; g++)
            {
                int cnt = _tempGroupCount[g];
                if (cnt > maxMatchCount)
                {
                    maxMatchCount = cnt;
                    maxMatchIdx = g;
                }
            }

            for (int g = 0; g < groupCount; g++)
            {
                int cnt = _tempGroupCount[g];
                int blockType = _tempGroupType[g];

                int[] mx = new int[cnt];
                int[] my = new int[cnt];
                int idx = 0;
                for (int i = 0; i < writeIdx && idx < cnt; i++)
                {
                    if (_tempMatchedGroup[i] == g)
                    {
                        mx[idx] = _tempMatchedX[i];
                        my[idx] = _tempMatchedY[i];
                        idx++;
                    }
                }

                int resultSpawnX = -1;
                int resultSpawnY = -1;
                int activatedSpec = 0;
                int activatedSpecX = -1;
                int activatedSpecY = -1;

                if (g == maxMatchIdx && swapX >= 0 && swapY >= 0)
                {
                    if (_enableColorClear && cnt >= _colorClearThreshold)
                    {
                        resultSpawnX = spawnX;
                        resultSpawnY = spawnY;
                    }
                    else if (_enableBomb && cnt >= _bombThreshold)
                    {
                        resultSpawnX = spawnX;
                        resultSpawnY = spawnY;
                    }
                }

                for (int i = 0; i < cnt; i++)
                {
                    int sx = _boardSpecials[mx[i], my[i]];
                    if (sx != 0)
                    {
                        activatedSpec = sx;
                        activatedSpecX = mx[i];
                        activatedSpecY = my[i];
                        break;
                    }
                }

                results[g] = new P002MatchResult
                {
                    BlockType = blockType,
                    MatchCount = cnt,
                    MatchedX = mx,
                    MatchedY = my,
                    SpawnX = resultSpawnX,
                    SpawnY = resultSpawnY,
                    ActivatedSpecialType = activatedSpec,
                    ActivatedSpecialX = activatedSpecX,
                    ActivatedSpecialY = activatedSpecY
                };
            }

            return results;
        }

        void ClearMatchedCells()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_boardMatched[x, y])
                    {
                        _boardTypes[x, y] = 0;
                        _boardSpecials[x, y] = 0;
                    }
                }
            }
        }

        void ApplyGravity()
        {
            for (int x = 0; x < _width; x++)
            {
                int writeY = 0;
                for (int readY = 0; readY < _height; readY++)
                {
                    if (_boardTypes[x, readY] != 0)
                    {
                        _boardTypes[x, writeY] = _boardTypes[x, readY];
                        _boardSpecials[x, writeY] = _boardSpecials[x, readY];
                        if (readY != writeY)
                        {
                            _boardTypes[x, readY] = 0;
                            _boardSpecials[x, readY] = 0;
                        }
                        writeY++;
                    }
                }
                for (int y = writeY; y < _height; y++)
                {
                    _boardTypes[x, y] = 0;
                    _boardSpecials[x, y] = 0;
                }
            }
        }

        void RefillEmptyCells()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_boardTypes[x, y] == 0)
                    {
                        _boardTypes[x, y] = PickRandomTypeNoMatch(x, y);
                        _boardSpecials[x, y] = 0;
                    }
                }
            }
        }

        bool HasValidMoves()
        {
            int fx, fy, tx, ty;
            return FindFirstSwappableMatch(out fx, out fy, out tx, out ty);
        }

        void SpawnSpecialsFromResults(P002MatchResult[] results)
        {
            if (results == null) return;

            for (int i = 0; i < results.Length; i++)
            {
                P002MatchResult r = results[i];
                if (r.SpawnX >= 0 && r.SpawnY >= 0 && IsInBounds(r.SpawnX, r.SpawnY))
                {
                    int spawnSpec = 0;
                    if (_enableColorClear && r.MatchCount >= _colorClearThreshold)
                    {
                        spawnSpec = (int)EP002SpecialBlockType.ColorClear;
                    }
                    else if (_enableBomb && r.MatchCount >= _bombThreshold)
                    {
                        spawnSpec = (int)EP002SpecialBlockType.Bomb;
                    }

                    if (spawnSpec != 0)
                    {
                        _boardTypes[r.SpawnX, r.SpawnY] = r.BlockType;
                        _boardSpecials[r.SpawnX, r.SpawnY] = spawnSpec;
                    }
                }
            }
        }
    }
}
