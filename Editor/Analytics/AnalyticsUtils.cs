using Unity.Services.Deployment.Editor.Shared.Logging;
using Unity.Services.Deployment.Editor.Shared.Threading;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.Services.Deployment.Editor.Analytics
{
    static class AnalyticsUtils
    {
        public static void RegisterEventDefault(string eventName, int version = 1)
        {
            Sync.RunNextUpdateOnMain(() =>
            {
                var result = EditorAnalytics.RegisterEventWithLimit(
                    eventName,
                    AnalyticsConstants.k_MaxEventPerHour,
                    AnalyticsConstants.k_MaxItems,
                    AnalyticsConstants.k_VendorKey,
                    version);

                Logger.LogVerbose($"Analytics: {eventName}.v{version} registered with result {result}");
            });
        }

        public static void LogVerbose(string eventName, int version, AnalyticsResult result)
        {
            Logger.LogVerbose($"Analytics Event: {eventName}.v{version}. Result: {result}");
        }
    }
}
