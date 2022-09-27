using Unity.Services.Deployment.Editor.Settings;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Interface.UI.Views
{
    class EnvironmentNotSetView : ViewBase
    {
        protected override string UxmlName => "DeploymentWindow_EnvironmentNotSet";

        public EnvironmentNotSetView()
        {
            var deploymentSettingsBtn = this.Q<Button>();
            deploymentSettingsBtn.clicked += OpenEnvironmentProvider;
        }

        static void OpenEnvironmentProvider()
        {
            SettingsService.OpenProjectSettings(DeploymentSettingsProvider.s_SettingsLocation);
        }

        new class UxmlFactory : UxmlFactory<EnvironmentNotSetView> {}
    }
}
