using UnityEngine;

namespace Game.Editor.AI
{
    public static class IntegrationStrategyEngine
    {
        const float REUSE_THRESHOLD = 0.9f;
        const float EXTEND_THRESHOLD = 0.7f;
        const float ADAPT_THRESHOLD = 0.55f;

        public struct ReuseDecision
        {
            public string Strategy;
            public string TargetModule;
            public string Reason;
            public string CompatibilityReview;
            public string ImpactAnalysis;
            public bool MigrationRequired;
            public string RiskLevel;
        }

        public static ReuseDecision Decide(
            ModuleDiscovery.DiscoveryResult discovery,
            bool targetIsEditable,
            string requestedFeatureDescription)
        {
            ReuseDecision decision = new ReuseDecision
            {
                Strategy = "create_new",
                TargetModule = null,
                Reason = "No similar modules found",
                CompatibilityReview = "not_required",
                ImpactAnalysis = "not_required",
                MigrationRequired = false,
                RiskLevel = "low"
            };

            if (discovery.CandidateCount == 0)
                return decision;

            ModuleDiscovery.CandidateModule topCandidate = discovery.Candidates[0];

            if (topCandidate.SimilarityScore >= REUSE_THRESHOLD)
            {
                decision.Strategy = "reuse";
                decision.TargetModule = topCandidate.ModuleName;
                decision.Reason = topCandidate.ModuleName
                    + " already provides the requested functionality (score: "
                    + topCandidate.SimilarityScore.ToString("F2") + ")";
                decision.CompatibilityReview = "passed";
                decision.ImpactAnalysis = "not_required";
                decision.MigrationRequired = false;
                decision.RiskLevel = "low";
                return decision;
            }

            if (topCandidate.SimilarityScore >= EXTEND_THRESHOLD)
            {
                if (targetIsEditable)
                {
                    decision.Strategy = "extend";
                    decision.TargetModule = topCandidate.ModuleName;
                    decision.Reason = topCandidate.ModuleName
                        + " covers partial functionality, extend with new APIs (score: "
                        + topCandidate.SimilarityScore.ToString("F2") + ")";
                    decision.CompatibilityReview = "pending";
                    decision.ImpactAnalysis = "pending";
                    decision.MigrationRequired = false;
                    decision.RiskLevel = "medium";
                }
                else
                {
                    decision.Strategy = "adapt";
                    decision.TargetModule = topCandidate.ModuleName;
                    decision.Reason = topCandidate.ModuleName
                        + " is not editable, wrapping with adapter (score: "
                        + topCandidate.SimilarityScore.ToString("F2") + ")";
                    decision.CompatibilityReview = "pending";
                    decision.ImpactAnalysis = "not_required";
                    decision.MigrationRequired = false;
                    decision.RiskLevel = "medium";
                }
                return decision;
            }

            if (topCandidate.SimilarityScore >= ADAPT_THRESHOLD)
            {
                decision.Strategy = "create_new";
                decision.TargetModule = null;
                decision.Reason = topCandidate.ModuleName
                    + " has some similarity but insufficient for reuse (score: "
                    + topCandidate.SimilarityScore.ToString("F2")
                    + "), creating new module with reference";
                decision.RiskLevel = "low";
                return decision;
            }

            return decision;
        }

        public static bool RequiresImpactAnalysis(string strategy)
        {
            return strategy == "extend" || strategy == "replace";
        }

        public static bool RequiresMigrationPlan(string strategy)
        {
            return strategy == "replace";
        }

        public static bool CanProceedToBuilder(ReuseDecision decision)
        {
            if (decision.Strategy == "replace")
            {
                if (decision.ImpactAnalysis != "completed")
                    return false;
                if (!decision.MigrationRequired)
                    return false;
            }

            if (decision.Strategy == "reuse")
                return false;

            return true;
        }

        [UnityEditor.MenuItem("Tools/AI/Run Reuse Decision (Test)")]
        static void RunDecisionTest()
        {
            string[] testKeywords = { "inventory", "item", "slot", "stack" };
            ModuleDiscovery.DiscoveryResult discovery =
                ModuleDiscovery.RunDiscovery("아이템 인벤토리 관리 시스템", testKeywords);

            ReuseDecision decision = Decide(discovery, true, "아이템 인벤토리 관리 시스템");

            Debug.Log("[Reuse Decision] Strategy: " + decision.Strategy);
            Debug.Log("[Reuse Decision] Target: " + (decision.TargetModule ?? "(none)"));
            Debug.Log("[Reuse Decision] Reason: " + decision.Reason);
            Debug.Log("[Reuse Decision] Compatibility: " + decision.CompatibilityReview);
            Debug.Log("[Reuse Decision] Impact Analysis: " + decision.ImpactAnalysis);
            Debug.Log("[Reuse Decision] Migration Required: " + decision.MigrationRequired);
            Debug.Log("[Reuse Decision] Risk: " + decision.RiskLevel);
        }
    }
}
