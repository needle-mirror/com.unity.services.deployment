using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Analytics.Environment;
using Unity.Services.Deployment.Editor.Configuration;
using Unity.Services.Deployment.Editor.Shared.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Environments.UI
{
    class EnvironmentSelector : VisualElement
    {
        static readonly string k_UxmlPath = Path.Combine(Constants.k_EditorRootPath, "Environments/UI/Assets/EnvironmentSelectorUI.uxml");

        IEnvironmentAnalytics m_EnvironmentAnalytics;
        IEnvironmentFetcher m_EnvironmentFetcher;
        IDeploymentSettings m_DeploymentSettings;
        EnvironmentDropdown m_EnvironmentDropdown;
        EnvironmentInfo m_EmptyEnvironment;

        Dictionary<string, EnvironmentInfo> m_EnvironmentMap;

        VisualElement m_ContainerDropdown;
        VisualElement m_ContainerFetching;
        VisualElement m_ContainerWarning;

        public void Bind(IEnvironmentAnalytics environmentAnalytics,
            IEnvironmentFetcher environmentFetcher,
            IDeploymentSettings deploymentSettings)
        {
            m_EnvironmentAnalytics = environmentAnalytics;
            m_EnvironmentFetcher = environmentFetcher;
            m_DeploymentSettings = deploymentSettings;
            m_EnvironmentDropdown = new EnvironmentDropdown(environmentAnalytics, deploymentSettings);
            m_EmptyEnvironment = new EnvironmentInfo() {Id = string.Empty, IsDefault = false, Name = string.Empty};
            m_EnvironmentMap = new Dictionary<string, EnvironmentInfo>
            {
                {m_EmptyEnvironment.Name, m_EmptyEnvironment}
            };

            Setup();
            RefreshUI();

            Sync.SafeAsync(async() =>
            {
                await FetchEnvironments();
                OnEnvironmentChanged(GetEnvironmentInfoForGuid(m_DeploymentSettings.EnvironmentGuid));
                Sync.RunNextUpdateOnMain(RefreshUI);
            });
        }

        void Setup()
        {
            m_EnvironmentAnalytics.RegisterEnvironmentChangedEvent();
            LoadUxml(this);
            SetupDropdown(this);
            SetupManageEnvironments(this);
            SetupWarning(this);
        }

        void LoadUxml(VisualElement containerElement)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            if (uxmlAsset != null)
            {
                uxmlAsset.CloneTree(containerElement);
            }
            else
            {
                throw new MissingReferenceException("Could not find a uxml asset to load.");
            }
        }

        void SetupDropdown(VisualElement containerElement)
        {
            m_ContainerDropdown = containerElement.Q(UxmlNames.ContainerDropdown);
            m_ContainerFetching = containerElement.Q(UxmlNames.ContainerFetching);
        }

        void SetupManageEnvironments(VisualElement containerElement)
        {
            var containerManageEnvironments = containerElement.Q(UxmlNames.ContainerManageEnvironments);
#if ENABLE_EDITOR_GAME_SERVICES
            var clickable = new Clickable(() =>
            {
                Application.OpenURL($"https://dashboard.unity3d.com/organizations/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/settings/environments");
            });
            containerManageEnvironments.AddManipulator(clickable);
#else
            containerManageEnvironments.style.display = DisplayStyle.None;
#endif
        }

        void SetupWarning(VisualElement containerElement)
        {
            m_ContainerWarning = containerElement.Q(UxmlNames.ContainerWarning);
            m_ContainerWarning.style.display = DisplayStyle.None;

            m_DeploymentSettings.PropertyChanged += OnEnvironmentGuidChanged;
        }

        void OnEnvironmentGuidChanged(object obj, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(m_DeploymentSettings.EnvironmentGuid))
            {
                OnEnvironmentChanged(GetEnvironmentInfoForGuid(m_DeploymentSettings.EnvironmentGuid));
            }
        }

        void OnEnvironmentChanged(EnvironmentInfo environmentInfo)
        {
            m_ContainerWarning.style.display = environmentInfo.IsDefault
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        EnvironmentInfo GetEnvironmentInfoForGuid(string environmentGuid)
        {
            return m_EnvironmentMap
                .Values
                .FirstOrDefault(info => info.Id == environmentGuid);
        }

        void RefreshUI()
        {
            SetVisibleContainer(m_EnvironmentDropdown.IsReady ? m_ContainerDropdown : m_ContainerFetching);
        }

        void SetVisibleContainer(VisualElement containerElement)
        {
            m_ContainerDropdown.style.display =
                containerElement == m_ContainerDropdown
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            m_ContainerFetching.style.display =
                containerElement == m_ContainerFetching
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        async Task FetchEnvironments()
        {
            m_EnvironmentMap = await m_EnvironmentFetcher.FetchEnvironments();

            if (m_EnvironmentMap == null)
            {
                m_EnvironmentMap = new Dictionary<string, EnvironmentInfo>();
            }

            m_EnvironmentMap.Add(m_EmptyEnvironment.Name, m_EmptyEnvironment);
            m_EnvironmentDropdown.Setup(this, m_EnvironmentMap);
        }

        static class UxmlNames
        {
            public const string ContainerDropdown = "Dropdown Section";
            public const string ContainerFetching = "Fetching Environments Section";
            public const string ContainerManageEnvironments = "Manage Environments Container";
            public const string ContainerWarning = "Default Environment Section";
        }
    }
}
