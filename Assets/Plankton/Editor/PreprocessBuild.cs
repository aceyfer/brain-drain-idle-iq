using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Plankton.Editor
{
    public class PreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -99999;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (PlanktonSettings.GeneratePreBuild)
                PlanktonSettings.GenerateFiles();
        }

    }
}