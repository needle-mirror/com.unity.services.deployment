using System.Collections.Generic;
using Unity.Services.Core.Editor;
using Unity.Services.Deployment.Editor.Analytics.Environment;
using Unity.Services.Deployment.Editor.Configuration;
using Unity.Services.Deployment.Editor.Environments;
using Unity.Services.Deployment.Editor.Environments.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Settings
{
    class DeploymentSettingsProvider : EditorGameServiceSettingsProvider
    {
        const string k_ServiceName = "Deployment";
        public static readonly string s_SettingsLocation = $"Project/Services/{k_ServiceName}";

        protected override IEditorGameService EditorGameService
            => EditorGameServiceRegistry.Instance.GetEditorGameService<DeploymentIdentifier>();

        protected override string Title { get; } = k_ServiceName;
        protected override string Description { get; } = "Move assets and configurations to backend services from within the editor";

        readonly IEnvironmentAnalytics m_Analytics;
        readonly IEnvironmentFetcher m_Fetcher;
        readonly IDeploymentSettings m_DeploymentSettings;

        DeploymentSettingsProvider(
            IEnvironmentAnalytics analytics,
            IEnvironmentFetcher environmentFetcher,
            IDeploymentSettings deploymentSettings,
            string path,
            SettingsScope scopes,
            IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_Analytics = analytics;
            m_Fetcher = environmentFetcher;
            m_DeploymentSettings = deploymentSettings;
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new DeploymentSettingsProvider(
                DeploymentServices.Instance.GetService<IEnvironmentAnalytics>(),
                DeploymentServices.Instance.GetService<IEnvironmentFetcher>(),
                DeploymentServices.Instance.GetService<IDeploymentSettings>(),
                GenerateProjectSettingsPath(k_ServiceName),
                SettingsScope.Project);
        }

        protected override VisualElement GenerateServiceDetailUI()
        {
            var environmentSelector = new EnvironmentSelector();
            environmentSelector.Bind(
                m_Analytics,
                m_Fetcher,
                m_DeploymentSettings);
            return environmentSelector;
        }

        protected override VisualElement GenerateUnsupportedDetailUI()
        {
            return GenerateServiceDetailUI();
        }
    }
}
