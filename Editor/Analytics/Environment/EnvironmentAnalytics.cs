using UnityEditor;

namespace Unity.Services.Deployment.Editor.Analytics.Environment
{
    class EnvironmentAnalytics : IEnvironmentAnalytics
    {
        const string k_EventNameOpenDeploymentWindow = "deployment_environmentchanged";
        const int k_VersionEnvironmentChanged = 1;

        public void RegisterEnvironmentChangedEvent()
        {
            AnalyticsUtils.RegisterEventDefault(k_EventNameOpenDeploymentWindow, k_VersionEnvironmentChanged);
        }

        public void SendEnvironmentChangedEvent(EnvironmentChangedParameters environmentChangedParameters)
        {
            var result = EditorAnalytics.SendEventWithLimit(k_EventNameOpenDeploymentWindow,
                environmentChangedParameters,
                k_VersionEnvironmentChanged);
            AnalyticsUtils.LogVerbose(k_EventNameOpenDeploymentWindow, k_VersionEnvironmentChanged, result);
        }
    }
}
