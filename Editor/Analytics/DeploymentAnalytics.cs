using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Services.Deployment.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;

namespace Unity.Services.Deployment.Editor.Analytics
{
    class DeploymentAnalytics : IDeploymentAnalytics
    {
        const string k_EventNameDeploy = "deployment_deployed";
        const int k_VersionDeploy = 2;

        readonly IEnvironmentService m_EnvironmentService;

        public DeploymentAnalytics(IEnvironmentService environmentService)
        {
            m_EnvironmentService = environmentService;
            AnalyticsUtils.RegisterEventDefault(k_EventNameDeploy, k_VersionDeploy);
        }

        public IDeploymentAnalytics.IDeployEvent BeginDeploy(IReadOnlyDictionary<string, List<IDeploymentItem>> itemsPerProvider, string source)
        {
            return new DeployEvent(m_EnvironmentService.ActiveEnvironmentId, itemsPerProvider, source);
        }

        class DeployEvent : IDeploymentAnalytics.IDeployEvent
        {
            readonly IReadOnlyDictionary<string, List<IDeploymentItem>> m_ItemsPerProvider;
            readonly Stopwatch m_Stopwatch = new();
            readonly Guid? m_Environment;
            readonly string m_Source;

            public DeployEvent(Guid? environment, IReadOnlyDictionary<string, List<IDeploymentItem>> itemsPerProvider, string source)
            {
                m_Environment = environment;
                m_ItemsPerProvider = itemsPerProvider;
                m_Source = source;
                m_Stopwatch.Start();
            }

            public void SendSuccess()
            {
                var result = EditorAnalytics.SendEventWithLimit(
                    k_EventNameDeploy, CreateDeployEvent("success"), k_VersionDeploy);
                AnalyticsUtils.LogVerbose(k_EventNameDeploy, k_VersionDeploy, result);
            }

            public void SendFailure(Exception exception)
            {
                var result = EditorAnalytics.SendEventWithLimit(
                    k_EventNameDeploy, CreateDeployEvent("failure", exception), k_VersionDeploy);
                AnalyticsUtils.LogVerbose(k_EventNameDeploy, k_VersionDeploy, result);
            }

            DeployEventPayload CreateDeployEvent(string status, Exception exception = null)
            {
                return new DeployEventPayload
                {
                    status = status,
                    environment = m_Environment?.ToString(),
                    duration = m_Stopwatch.ElapsedMilliseconds,
                    exception = exception?.GetType().ToString(),
                    source = m_Source,
                    providers = m_ItemsPerProvider.Keys.Select(name =>
                        new DeployEventPayload.Provider
                        {
                            name = name,
                            item_count = m_ItemsPerProvider[name].Count,
                            failure_count = m_ItemsPerProvider[name].Count(i => i.Status.MessageSeverity == SeverityLevel.Error)
                        }
                        ).ToList()
                };
            }
        }

        [Serializable]
        // Naming exception to the standard in order to match the schema
        // ReSharper disable InconsistentNaming
        struct DeployEventPayload
        {
            public string environment;
            public string status;
            public string exception;
            public string source;
            public long duration;
            public List<Provider> providers;

            [Serializable]
            public struct Provider
            {
                public string name;
                public int item_count;
                public int failure_count;
            }
        }
        // ReSharper restore InconsistentNaming
    }
}
