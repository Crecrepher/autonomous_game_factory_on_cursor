using System;

namespace Game
{
    public class P002SkillSystemRuntime : IP002SkillSystem
    {
        const int CHARACTER_COUNT = 3;
        const int PENDING_QUEUE_SIZE = 3;

        readonly P002SkillSystemConfig _config;
        IP002GameConfig _gameConfig;

        int[] _gauges;
        int[] _gaugeMax;
        bool[] _isReady;
        int[] _pendingQueue;
        int _pendingHead;
        int _pendingCount;
        float[] _autoTimers;
        bool[] _autoWaiting;
        bool _isExecuting;
        bool _isBlocked;
        EP002SkillMode _skillMode;

        public event Action<int> OnSkillReady;
        public event Action<int> OnSkillExecuted;

        public P002SkillSystemRuntime(P002SkillSystemConfig config)
        {
            _config = config;
        }

        public void Init(IP002GameConfig config)
        {
            _gameConfig = config;

            _gauges = new int[CHARACTER_COUNT];
            _gaugeMax = new int[CHARACTER_COUNT];
            _isReady = new bool[CHARACTER_COUNT];
            _pendingQueue = new int[PENDING_QUEUE_SIZE];
            _pendingHead = 0;
            _pendingCount = 0;
            _autoTimers = new float[CHARACTER_COUNT];
            _autoWaiting = new bool[CHARACTER_COUNT];
            _isExecuting = false;
            _isBlocked = false;

            int gaugeMax = _gameConfig != null ? _gameConfig.SkillGaugeMax : _config.SkillGaugeMax;

            for (int i = 0; i < CHARACTER_COUNT; i++)
            {
                _gaugeMax[i] = gaugeMax;
                _gauges[i] = _gameConfig != null ? _gameConfig.GetInitialGauge(i) : 0;
                _isReady[i] = false;
                _autoTimers[i] = 0f;
                _autoWaiting[i] = false;

                if (_gauges[i] >= _gaugeMax[i])
                {
                    _gauges[i] = _gaugeMax[i];
                    _isReady[i] = true;
                }
            }

            _skillMode = _gameConfig != null ? _gameConfig.SkillMode : _config.SkillMode;
        }

        public void Tick(float deltaTime)
        {
            if (_skillMode != EP002SkillMode.Auto)
                return;
            if (_isExecuting || _isBlocked)
                return;

            float autoDelay = _gameConfig != null ? _gameConfig.AutoSkillDelay : _config.AutoSkillDelay;

            for (int i = 0; i < CHARACTER_COUNT; i++)
            {
                if (!_autoWaiting[i])
                    continue;

                _autoTimers[i] += deltaTime;
                if (_autoTimers[i] >= autoDelay)
                {
                    _autoTimers[i] = 0f;
                    _autoWaiting[i] = false;
                    ExecuteSkillInternal(i);
                }
            }
        }

        public bool TryActivateSkill(int characterIndex)
        {
            if (_isBlocked || _isExecuting)
                return false;
            if (characterIndex < 0 || characterIndex >= CHARACTER_COUNT)
                return false;
            if (!_isReady[characterIndex])
                return false;

            if (_skillMode == EP002SkillMode.Auto)
            {
                EnqueuePending(characterIndex);
                if (!_isExecuting && _pendingCount > 0)
                {
                    int next = DequeuePending();
                    return ExecuteSkillInternal(next);
                }
                return true;
            }

            return ExecuteSkillInternal(characterIndex);
        }

        public void OnSkillAnimationComplete(int characterIndex)
        {
            _isExecuting = false;

            if (_pendingCount > 0)
            {
                int next = DequeuePending();
                if (next >= 0)
                    ExecuteSkillInternal(next);
            }
        }

        public void SetBlocked(bool blocked)
        {
            _isBlocked = blocked;
        }

        public float GetGaugeRatio(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex >= CHARACTER_COUNT)
                return 0f;
            int maxVal = _gaugeMax[characterIndex];
            if (maxVal <= 0) return 0f;
            float invMax = 1f / maxVal;
            return _gauges[characterIndex] * invMax;
        }

        public bool IsSkillReady(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex >= CHARACTER_COUNT)
                return false;
            return _isReady[characterIndex];
        }

        public void AddGauge(int characterIndex, int pieceCount)
        {
            if (characterIndex < 0 || characterIndex >= CHARACTER_COUNT)
                return;
            if (pieceCount <= 0)
                return;

            int gaugePerPiece = _gameConfig != null ? _gameConfig.GaugePerPiece : _config.GaugePerPiece;
            int add = pieceCount * gaugePerPiece;
            int newVal = _gauges[characterIndex] + add;
            int maxVal = _gaugeMax[characterIndex];

            if (newVal >= maxVal)
            {
                newVal = maxVal;
                _gauges[characterIndex] = newVal;
                if (!_isReady[characterIndex])
                {
                    _isReady[characterIndex] = true;
                    if (_skillMode == EP002SkillMode.Auto)
                    {
                        _autoWaiting[characterIndex] = true;
                        _autoTimers[characterIndex] = 0f;
                    }
                    if (OnSkillReady != null)
                        OnSkillReady.Invoke(characterIndex);
                }
            }
            else
            {
                _gauges[characterIndex] = newVal;
            }
        }

        bool ExecuteSkillInternal(int characterIndex)
        {
            if (_isExecuting || !_isReady[characterIndex])
                return false;

            _isExecuting = true;
            _gauges[characterIndex] = 0;
            _isReady[characterIndex] = false;

            if (OnSkillExecuted != null)
                OnSkillExecuted.Invoke(characterIndex);

            return true;
        }

        void EnqueuePending(int characterIndex)
        {
            if (_pendingCount >= PENDING_QUEUE_SIZE)
                return;
            int tail = (_pendingHead + _pendingCount) % PENDING_QUEUE_SIZE;
            _pendingQueue[tail] = characterIndex;
            _pendingCount++;
        }

        int DequeuePending()
        {
            if (_pendingCount <= 0)
                return -1;
            int idx = _pendingQueue[_pendingHead];
            _pendingHead = (_pendingHead + 1) % PENDING_QUEUE_SIZE;
            _pendingCount--;
            return idx;
        }
    }
}
