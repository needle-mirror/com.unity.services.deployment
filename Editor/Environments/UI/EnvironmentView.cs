using System.IO;
using Unity.Services.Deployment.Editor.Settings;
using Unity.Services.Deployment.Editor.Shared.UI;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Environments.UI
{
    class EnvironmentView : VisualElement
    {
        static readonly string k_TemplatePath = Path.Combine(
            Constants.k_EditorRootPath,
            "Environments/UI/Assets/EnvironmentView.uxml");
        static readonly string k_StylePath = Path.Combine(
            Constants.k_EditorRootPath,
            "Environments/UI/Assets/EnvironmentView_style.uss");

        static readonly string k_EnvironmentSettingsMenuItemText = L10n.Tr("Environment Settings");
        static readonly string k_EnvironmentNotSet = L10n.Tr("Environment not set");
        const string k_IconClass = "environment-icon";
        internal const string k_WarningClass = "warning";

        readonly ModelBinding<IEnvironmentService> m_EnvironmentBinding;

        public EnvironmentView()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath));

            visualTreeAsset.CloneTree(this);
            var toolbarMenu = this.Q<ToolbarMenu>();
            toolbarMenu.menu.AppendAction(
                k_EnvironmentSettingsMenuItemText,
                _ => OpenEnvironmentSettings());
            var icon = new VisualElement();
            icon.AddToClassList(k_IconClass);
            toolbarMenu.Insert(0, icon);

            m_EnvironmentBinding = new ModelBinding<IEnvironmentService>(this);
            m_EnvironmentBinding.BindProperty(nameof(IEnvironmentService.Environments), value =>
            {
                UpdateBindings(value, false);
            });
            m_EnvironmentBinding.BindProperty(nameof(IEnvironmentService.ActiveEnvironmentId), value =>
            {
                UpdateBindings(value, true);
            });
        }

        public void Bind(IEnvironmentService environmentService)
        {
            m_EnvironmentBinding.Source = environmentService;
        }

        void UpdateBindings(IEnvironmentService service, bool shouldRequestRequery = false)
        {
            var menu = this.Q<ToolbarMenu>();

            var activeEnvironment = service.ActiveEnvironmentInfo();

            var validEnv = activeEnvironment != null;
            menu.text = validEnv ? activeEnvironment.Value.Name : k_EnvironmentNotSet;
            if (!validEnv && shouldRequestRequery)
            {
                service.RefreshAsync();
            }

            if (activeEnvironment?.IsDefault ?? false)
            {
                AddToClassList(k_WarningClass);
            }
            else
            {
                RemoveFromClassList(k_WarningClass);
            }
        }

        internal EditorWindow OpenEnvironmentSettings()
        {
            return SettingsService.OpenProjectSettings(DeploymentSettingsProvider.s_SettingsLocation);
        }

        new class UxmlFactory : UxmlFactory<EnvironmentView> {}
    }
}
