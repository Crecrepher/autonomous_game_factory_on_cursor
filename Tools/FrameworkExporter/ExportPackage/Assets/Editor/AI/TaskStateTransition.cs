using UnityEngine;

namespace Game.Editor.AI
{
    public static class TaskStateTransition
    {
        const string LOG_PREFIX = "[TaskStateTransition] ";

        const int MAX_RETRY_COUNT = 3;

        public struct TransitionResult
        {
            public bool Allowed;
            public string Reason;
        }

        public static TransitionResult ValidateStatusTransition(
            DependencyGraphBuilder.TaskEntry task, string targetStatus, DependencyGraphBuilder.DependencyGraph graph)
        {
            TransitionResult result = new TransitionResult();

            if (targetStatus == "planned" && task.Status == "pending")
            {
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "in_progress" && task.Status == "planned")
            {
                for (int d = 0; d < task.DependsOn.Length; d++)
                {
                    DependencyGraphBuilder.TaskEntry dep;
                    if (!graph.TaskMap.TryGetValue(task.DependsOn[d], out dep) || dep.Status != "done")
                    {
                        result.Allowed = false;
                        result.Reason = "depends_on '" + task.DependsOn[d] + "' is not done";
                        return result;
                    }
                }
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "in_progress" && task.Status == "blocked")
            {
                if (task.RetryCount >= MAX_RETRY_COUNT)
                {
                    result.Allowed = false;
                    result.Reason = "retry_count >= " + MAX_RETRY_COUNT + " — must escalate";
                    return result;
                }
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "review" && task.Status == "in_progress")
            {
                if (task.HumanState != "validated")
                {
                    result.Allowed = false;
                    result.Reason = "human_state must be 'validated' (Human Gate)";
                    return result;
                }
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "blocked" && task.Status == "review")
            {
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "done" && task.Status == "review")
            {
                if (task.CommitState != "committed" && task.CommitState != "recommitted")
                {
                    result.Allowed = false;
                    result.Reason = "commit_state must be 'committed' or 'recommitted'";
                    return result;
                }
                if (task.LearningState != "recorded")
                {
                    result.Allowed = false;
                    result.Reason = "learning_state must be 'recorded'";
                    return result;
                }
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "escalated" && task.Status == "blocked")
            {
                if (task.RetryCount < MAX_RETRY_COUNT)
                {
                    result.Allowed = false;
                    result.Reason = "retry_count (" + task.RetryCount + ") < " + MAX_RETRY_COUNT;
                    return result;
                }
                result.Allowed = true;
                return result;
            }

            if (targetStatus == "blocked" && task.Status == "done")
            {
                result.Allowed = true;
                return result;
            }

            result.Allowed = false;
            result.Reason = "Invalid transition: " + task.Status + " → " + targetStatus;
            return result;
        }

        public static TransitionResult ValidateHumanStateTransition(
            DependencyGraphBuilder.TaskEntry task, string targetHumanState)
        {
            TransitionResult result = new TransitionResult();
            string current = task.HumanState;

            if (current == "none" && targetHumanState == "pending")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "pending" && targetHumanState == "in_review")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "in_review" && (targetHumanState == "validated" || targetHumanState == "fixing"))
            {
                result.Allowed = true;
                return result;
            }

            if (current == "fixing" && targetHumanState == "validated")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "validated" && targetHumanState == "pending")
            {
                result.Allowed = true;
                return result;
            }

            result.Allowed = false;
            result.Reason = "Invalid human_state transition: " + current + " → " + targetHumanState;
            return result;
        }

        public static TransitionResult ValidateCommitStateTransition(
            DependencyGraphBuilder.TaskEntry task, string targetCommitState)
        {
            TransitionResult result = new TransitionResult();
            string current = task.CommitState;

            if (current == "none" && targetCommitState == "ready")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "ready" && (targetCommitState == "committed" || targetCommitState == "recommitted"))
            {
                result.Allowed = true;
                return result;
            }

            if (current == "committed" && targetCommitState == "recommit_ready")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "recommit_ready" && targetCommitState == "ready")
            {
                result.Allowed = true;
                return result;
            }

            result.Allowed = false;
            result.Reason = "Invalid commit_state transition: " + current + " → " + targetCommitState;
            return result;
        }

        public static TransitionResult ValidateLearningStateTransition(
            DependencyGraphBuilder.TaskEntry task, string targetLearningState)
        {
            TransitionResult result = new TransitionResult();
            string current = task.LearningState;

            if (current == "none" && targetLearningState == "pending")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "pending" && targetLearningState == "recorded")
            {
                result.Allowed = true;
                return result;
            }

            if (current == "recorded" && targetLearningState == "pending")
            {
                result.Allowed = true;
                return result;
            }

            result.Allowed = false;
            result.Reason = "Invalid learning_state transition: " + current + " → " + targetLearningState;
            return result;
        }

        public static DependencyGraphBuilder.TaskEntry ApplyBlockedReset(DependencyGraphBuilder.TaskEntry task)
        {
            task.Status = "in_progress";
            task.HumanState = "pending";
            task.LearningState = "none";
            task.CommitState = "none";
            task.RetryCount = task.RetryCount + 1;
            Debug.Log(LOG_PREFIX + task.Name + " blocked→in_progress reset (retry " + task.RetryCount + ")");
            return task;
        }

        public static DependencyGraphBuilder.TaskEntry ApplyCommittedAutoTransition(DependencyGraphBuilder.TaskEntry task)
        {
            task.LearningState = "pending";
            Debug.Log(LOG_PREFIX + task.Name + " commit detected — learning_state → pending");
            return task;
        }
    }
}
