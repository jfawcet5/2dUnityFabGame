namespace BeyProject.Data
{
    public enum ProcessorBehaviorType
    {
        Standard,
        ParallelProcessing,
        FocusingAlgorithm,
        PredictiveTargeting,

        // Appended rather than reordered - existing Item_*.asset files already have the
        // values above serialized as plain ints, so inserting here would silently reclassify
        // them. These three are read by PlayerCombat to dispatch qualitatively different
        // firing patterns, not just different stat multipliers.
        Burst,
        Scatter,
        Charge
    }
}
