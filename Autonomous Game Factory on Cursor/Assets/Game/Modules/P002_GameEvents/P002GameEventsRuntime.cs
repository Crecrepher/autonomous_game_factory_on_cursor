using System;

namespace Game
{
    public class P002GameEventsRuntime : IP002GameEvents
    {
        public event Action<int, int> OnPuzzleMatched;
        public event Action OnBoardRefilled;
        public event Action<int, int> OnAttackHit;
        public event Action<int, int, int> OnMonsterDamaged;
        public event Action<int> OnMonsterDead;
        public event Action<int> OnNewMonsterSpawned;
        public event Action<int> OnSkillGaugeFull;
        public event Action<int, int> OnSkillGaugeChanged;
        public event Action<int> OnSkillActivated;
        public event Action<int> OnSkillCompleted;
        public event Action<int, int, int> OnSkillBurstStep;
        public event Action<int> OnCharacterAttack;
        public event Action<int> OnCharacterSkillReady;
        public event Action<int> OnStageTransitionStart;
        public event Action<int> OnStageTransitionComplete;
        public event Action OnGameStarted;
        public event Action<bool> OnGameEnded;
        public event Action OnFirstInteraction;
        public event Action OnInputDetected;
        public event Action OnNoMovesAvailable;
        public event Action OnBlockDrop;
        public event Action OnBlockDestroy;
        public event Action OnSwapSuccess;
        public event Action OnSwapFail;
        public event Action<int, int> OnSpecialBlockSpawned;
        public event Action OnBombItemActivated;
        public event Action OnColorClearItemActivated;

        public void RaisePuzzleMatched(int x, int y)
        {
            if (OnPuzzleMatched != null) OnPuzzleMatched.Invoke(x, y);
        }

        public void RaiseBoardRefilled()
        {
            if (OnBoardRefilled != null) OnBoardRefilled.Invoke();
        }

        public void RaiseAttackHit(int attackerIndex, int targetIndex)
        {
            if (OnAttackHit != null) OnAttackHit.Invoke(attackerIndex, targetIndex);
        }

        public void RaiseMonsterDamaged(int monsterIndex, int damage, int remainingHealth)
        {
            if (OnMonsterDamaged != null) OnMonsterDamaged.Invoke(monsterIndex, damage, remainingHealth);
        }

        public void RaiseMonsterDead(int monsterIndex)
        {
            if (OnMonsterDead != null) OnMonsterDead.Invoke(monsterIndex);
        }

        public void RaiseNewMonsterSpawned(int monsterIndex)
        {
            if (OnNewMonsterSpawned != null) OnNewMonsterSpawned.Invoke(monsterIndex);
        }

        public void RaiseSkillGaugeFull(int characterIndex)
        {
            if (OnSkillGaugeFull != null) OnSkillGaugeFull.Invoke(characterIndex);
        }

        public void RaiseSkillGaugeChanged(int characterIndex, int gauge)
        {
            if (OnSkillGaugeChanged != null) OnSkillGaugeChanged.Invoke(characterIndex, gauge);
        }

        public void RaiseSkillActivated(int characterIndex)
        {
            if (OnSkillActivated != null) OnSkillActivated.Invoke(characterIndex);
        }

        public void RaiseSkillCompleted(int characterIndex)
        {
            if (OnSkillCompleted != null) OnSkillCompleted.Invoke(characterIndex);
        }

        public void RaiseSkillBurstStep(int characterIndex, int step, int totalSteps)
        {
            if (OnSkillBurstStep != null) OnSkillBurstStep.Invoke(characterIndex, step, totalSteps);
        }

        public void RaiseCharacterAttack(int characterIndex)
        {
            if (OnCharacterAttack != null) OnCharacterAttack.Invoke(characterIndex);
        }

        public void RaiseCharacterSkillReady(int characterIndex)
        {
            if (OnCharacterSkillReady != null) OnCharacterSkillReady.Invoke(characterIndex);
        }

        public void RaiseStageTransitionStart(int stageIndex)
        {
            if (OnStageTransitionStart != null) OnStageTransitionStart.Invoke(stageIndex);
        }

        public void RaiseStageTransitionComplete(int stageIndex)
        {
            if (OnStageTransitionComplete != null) OnStageTransitionComplete.Invoke(stageIndex);
        }

        public void RaiseGameStarted()
        {
            if (OnGameStarted != null) OnGameStarted.Invoke();
        }

        public void RaiseGameEnded(bool won)
        {
            if (OnGameEnded != null) OnGameEnded.Invoke(won);
        }

        public void RaiseFirstInteraction()
        {
            if (OnFirstInteraction != null) OnFirstInteraction.Invoke();
        }

        public void RaiseInputDetected()
        {
            if (OnInputDetected != null) OnInputDetected.Invoke();
        }

        public void RaiseNoMovesAvailable()
        {
            if (OnNoMovesAvailable != null) OnNoMovesAvailable.Invoke();
        }

        public void RaiseBlockDrop()
        {
            if (OnBlockDrop != null) OnBlockDrop.Invoke();
        }

        public void RaiseBlockDestroy()
        {
            if (OnBlockDestroy != null) OnBlockDestroy.Invoke();
        }

        public void RaiseSwapSuccess()
        {
            if (OnSwapSuccess != null) OnSwapSuccess.Invoke();
        }

        public void RaiseSwapFail()
        {
            if (OnSwapFail != null) OnSwapFail.Invoke();
        }

        public void RaiseSpecialBlockSpawned(int x, int y)
        {
            if (OnSpecialBlockSpawned != null) OnSpecialBlockSpawned.Invoke(x, y);
        }

        public void RaiseBombItemActivated()
        {
            if (OnBombItemActivated != null) OnBombItemActivated.Invoke();
        }

        public void RaiseColorClearItemActivated()
        {
            if (OnColorClearItemActivated != null) OnColorClearItemActivated.Invoke();
        }

        public void ClearAll()
        {
            OnPuzzleMatched = null;
            OnBoardRefilled = null;
            OnAttackHit = null;
            OnMonsterDamaged = null;
            OnMonsterDead = null;
            OnNewMonsterSpawned = null;
            OnSkillGaugeFull = null;
            OnSkillGaugeChanged = null;
            OnSkillActivated = null;
            OnSkillCompleted = null;
            OnSkillBurstStep = null;
            OnCharacterAttack = null;
            OnCharacterSkillReady = null;
            OnStageTransitionStart = null;
            OnStageTransitionComplete = null;
            OnGameStarted = null;
            OnGameEnded = null;
            OnFirstInteraction = null;
            OnInputDetected = null;
            OnNoMovesAvailable = null;
            OnBlockDrop = null;
            OnBlockDestroy = null;
            OnSwapSuccess = null;
            OnSwapFail = null;
            OnSpecialBlockSpawned = null;
            OnBombItemActivated = null;
            OnColorClearItemActivated = null;
        }
    }
}
