namespace Game
{
    public interface IP002GameEvents
    {
        void RaisePuzzleMatched(int x, int y);
        void RaiseBoardRefilled();
        void RaiseAttackHit(int attackerIndex, int targetIndex);
        void RaiseMonsterDamaged(int monsterIndex, int damage, int remainingHealth);
        void RaiseMonsterDead(int monsterIndex);
        void RaiseNewMonsterSpawned(int monsterIndex);
        void RaiseSkillGaugeFull(int characterIndex);
        void RaiseSkillGaugeChanged(int characterIndex, int gauge);
        void RaiseSkillActivated(int characterIndex);
        void RaiseSkillCompleted(int characterIndex);
        void RaiseSkillBurstStep(int characterIndex, int step, int totalSteps);
        void RaiseCharacterAttack(int characterIndex);
        void RaiseCharacterSkillReady(int characterIndex);
        void RaiseStageTransitionStart(int stageIndex);
        void RaiseStageTransitionComplete(int stageIndex);
        void RaiseGameStarted();
        void RaiseGameEnded(bool won);
        void RaiseFirstInteraction();
        void RaiseInputDetected();
        void RaiseNoMovesAvailable();
        void RaiseBlockDrop();
        void RaiseBlockDestroy();
        void RaiseSwapSuccess();
        void RaiseSwapFail();
        void RaiseSpecialBlockSpawned(int x, int y);
        void RaiseBombItemActivated();
        void RaiseColorClearItemActivated();
        void ClearAll();
    }
}
