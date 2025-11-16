namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Centralized constants for genetic system to avoid magic numbers
    /// </summary>
    public static class GeneticConstants
    {
        // Mutation parameters
        public const float DefaultMutationRate = 0.02f;
        public const float MildMutationMin = 0.1f;
        public const float MildMutationMax = 0.3f;
        public const float HarmfulMutationChance = 0.3f;
        public const float NovelMutationRarityFactor = 0.1f;

        // Genetic values
        public const float DefaultTraitValue = 0.5f;
        public const float SignificantTraitThreshold = 0.7f;
        public const float NeutralModifierBase = 0.5f;

        // Inheritance parameters
        public const float DominanceBlendFactor = 0.5f;
        public const float PolygenicReductionFactor = 0.8f;
        public const float EpistaticMaxModifier = 0.2f;

        // Environmental parameters
        public const float DefaultPredatorPressure = 0.5f;
        public const float DefaultSocialDensity = 0.5f;
        public const float StressActivationThreshold = 0.7f;
        public const float StressActivationChanceFactor = 0.3f;

        // Random generation ranges
        public const float RandomDominanceMin = 0.3f;
        public const float RandomDominanceMax = 0.8f;
        public const float RandomValueMin = 0.2f;
        public const float RandomValueMax = 0.9f;
        public const float DominanceChangeRange = 0.2f;

        // Ancient traits
        public const float AncientTraitDominanceMin = 0.7f;
        public const float AncientTraitDominanceMax = 0.9f;
        public const float AncientTraitRecessiveMin = 0.1f;
        public const float AncientTraitRecessiveMax = 0.3f;
        public const float AncientTraitExpressionMin = 0.8f;
        public const float AncientTraitExpressionMax = 1.0f;
        public const int AncientTraitGenerationMarker = -1;

        // Purity calculation
        public const float PurityMutationDenominator = 5f;
    }
}
