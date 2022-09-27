using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Unity.Services.Deployment.Editor.Configuration;
using Unity.Services.Deployment.Editor.Settings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Environments.UI
{
    class EnvironmentView : VisualElement
    {
        static readonly string s_TemplatePath = Path.Combine(
            Constants.k_EditorRootPath,
            "Environments/UI/Assets/EnvironmentView.uxml");
        static readonly string s_StylePath = Path.Combine(
            Constants.k_EditorRootPath,
            "Environments/UI/Assets/EnvironmentView_style.uss");

        static readonly string s_EnvironmentSettingsMenuItemText = L10n.Tr("Environment Settings");
        const string k_IconClass = "environment-icon";
        internal const string k_WarningClass = "warning";

        IDeploymentSettings m_DeploymentSettings;
        Dictionary<string, EnvironmentInfo> m_EnvironmentInfos;
        [NonSerialized]
        bool m_Bound;

        public event Action RequestRequery;

        public EnvironmentView()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(s_TemplatePath);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_StylePath));

            visualTreeAsset.CloneTree(this);
            var toolbarMenu = this.Q<ToolbarMenu>();
            toolbarMenu.menu.AppendAction(
                s_EnvironmentSettingsMenuItemText,
                _ => OpenEnvironmentSettings());
            m_EnvironmentInfos = new Dictionary<string, EnvironmentInfo>();
            var icon = new VisualElement();
            icon.AddToClassList(k_IconClass);
            toolbarMenu.Insert(0, icon);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (m_DeploymentSettings != null)
                {
                    Bind(m_DeploymentSettings);
                    UpdateBindings();
                }
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (m_DeploymentSettings != null)
                {
                    Unbind();
                }
            });
        }

        public void Bind(IDeploymentSettings settings, Dictionary<string, EnvironmentInfo> environments)
        {
            m_EnvironmentInfos = environments ?? new Dictionary<string, EnvironmentInfo>();
            Bind(settings);
            UpdateBindings();
        }

        public void UpdateEnvironments(Dictionary<string, EnvironmentInfo> environments)
        {
            m_EnvironmentInfos = environments ?? new Dictionary<string, EnvironmentInfo>();
            UpdateBindings();
        }

        void Bind(IDeploymentSettings settings)
        {
            if (m_Bound)
                return;
            m_DeploymentSettings = settings;
            m_DeploymentSettings.PropertyChanged += PropertyChangedOnPropertyChanged;
            m_Bound = true;
        }

        void UpdateBindings(bool shouldRequestRequery = false)
        {
            var menu = this.Q<ToolbarMenu>();
            string guid = m_DeploymentSettings.EnvironmentGuid;
            KeyValuePair<string, EnvironmentInfo> env = m_EnvironmentInfos
                .FirstOrDefault(kvp => kvp.Value.Id == guid);

            bool validEnv = !string.IsNullOrEmpty(env.Key);
            menu.text = validEnv ? env.Key : L10n.Tr("Environment not set");
            if (!validEnv && shouldRequestRequery)
            {
                RequestRequery?.Invoke();
            }

            if (env.Value.IsDefault)
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

        void PropertyChangedOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDeploymentSettings.EnvironmentGuid))
            {
                UpdateBindings(true);
            }
        }

        public void Unbind()
        {
            m_DeploymentSettings.PropertyChanged -= PropertyChangedOnPropertyChanged;
            RequestRequery = null;
            m_Bound = false;
        }

        new class UxmlFactory : UxmlFactory<EnvironmentView> {}
    }
}
