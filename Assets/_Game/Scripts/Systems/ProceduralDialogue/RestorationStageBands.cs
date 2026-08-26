namespace BrainDrain.Systems
{
    /// <summary>
    /// Canonical RestorationPercent band table for the 6 procedural-dialogue "stages" (0-5),
    /// as defined in PROCEDURAL_DIALOGUE_SPEC.md's "Stage" section. Authored minStage/maxStage
    /// values are resolved through this table into an actual RestorationPercent range at load
    /// time, so gating stays on the single RestorationPercent axis DialogueManager.TryFireLine
    /// already uses -- never a second, parallel gating axis.
    /// </summary>
    public static class RestorationStageBands
    {
        public const int StageCount = 6;

        private static readonly (float min, float max)[] Bands =
        {
            (0f, 16f),
            (17f, 33f),
            (34f, 50f),
            (51f, 67f),
            (68f, 84f),
            (85f, 100f),
        };

        /// <summary>Resolves an authored stage index (0-5) to its [min,max] RestorationPercent range.</summary>
        public static bool TryGetRange(int stage, out float minRestorationPercent, out float maxRestorationPercent)
        {
            if (stage < 0 || stage >= Bands.Length)
            {
                minRestorationPercent = 0f;
                maxRestorationPercent = 0f;
                return false;
            }

            (minRestorationPercent, maxRestorationPercent) = Bands[stage];
            return true;
        }

        /// <summary>
        /// Which stage band a single RestorationPercent value falls into. Used only by the
        /// migration tooling (PROCEDURAL_DIALOGUE_SPEC.md "Migrate existing dialogue") to
        /// derive minStage/maxStage from an existing NarratorLine's
        /// minRestorationPercent/maxRestorationPercent -- not part of runtime gating.
        /// </summary>
        public static int StageIndexForPercent(float percent)
        {
            for (int i = 0; i < Bands.Length; i++)
            {
                if (percent >= Bands[i].min && percent <= Bands[i].max)
                {
                    return i;
                }
            }

            return percent < Bands[0].min ? 0 : Bands.Length - 1;
        }

        /// <summary>Derives a [minStage,maxStage] index range covering an existing [minPercent,maxPercent] range.</summary>
        public static void ComputeStageRange(float minPercent, float maxPercent, out int minStage, out int maxStage)
        {
            minStage = StageIndexForPercent(minPercent);
            maxStage = StageIndexForPercent(maxPercent);
        }
    }
}
