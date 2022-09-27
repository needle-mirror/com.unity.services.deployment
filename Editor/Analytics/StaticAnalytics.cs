using UnityEditor;

namespace Unity.Services.Deployment.Editor.Analytics
{
    static class StaticAnalytics
    {
        const string k_EventNameOpenDeploymentWindow = "deployment_windowopened";
        const int k_VersionOpen = 1;

        public static void RegisterEvents()
        {
            AnalyticsUtils.RegisterEventDefault(k_EventNameOpenDeploymentWindow, k_VersionOpen);
        }

        public static void SendOpenedEvent()
        {
            var result = EditorAnalytics.SendEventWithLimit(k_EventNameOpenDeploymentWindow, null, k_VersionOpen);
            AnalyticsUtils.LogVerbose(k_EventNameOpenDeploymentWindow, k_VersionOpen, result);
        }
    }
}
