using System.Collections.Generic;
using Unity.Services.Core.Editor;
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

        readonly IEnvironmentService m_EnvironmentService;

        DeploymentSettingsProvider(
            IEnvironmentService environmentService,
            string path,
            SettingsScope scopes,
            IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_EnvironmentService = environmentService;
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new DeploymentSettingsProvider(
                DeploymentServices.Instance.GetService<IEnvironmentService>(),
                GenerateProjectSettingsPath(k_ServiceName),
                SettingsScope.Project);
        }

        protected override VisualElement GenerateServiceDetailUI()
        {
            var environmentSelector = new EnvironmentSelector();
            environmentSelector.Bind(m_EnvironmentService);
            return environmentSelector;
        }

        protected override VisualElement GenerateUnsupportedDetailUI()
        {
            return GenerateServiceDetailUI();
        }
    }
}
