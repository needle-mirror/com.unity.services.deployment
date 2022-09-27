using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Analytics;
using Unity.Services.Deployment.Editor.Commands;
using Unity.Services.Deployment.Editor.Configuration;
using Unity.Services.Deployment.Editor.DeploymentDefinitions;
using Unity.Services.Deployment.Editor.Environments;
using Unity.Services.Deployment.Editor.Environments.UI;
using Unity.Services.Deployment.Editor.Interface.UI.Views;
using Unity.Services.Deployment.Editor.Shared.IO;
using Unity.Services.Deployment.Editor.Shared.Threading;
using Unity.Services.Deployment.Editor.Shared.Tracking;
using Unity.Services.Deployment.Editor.State;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Interface.UI
{
    class DeploymentWindow : AuthoringWindow
    {
        const string k_DeploymentWindowTemplateName = "DeploymentWindow_uxml.uxml";
        const string k_HiddenClassName = "hidden";
        const string k_IconPath = "Icons/Deployment.png";
        internal const string k_EmptyListStateName = "EmptyListState";
        internal const string k_WindowTitle = "Deployment";

#if UNITY_2022_1_OR_NEWER
        internal const string k_DeploymentMenuItemPath = "Services/Deployment";
#else
        internal const string k_DeploymentMenuItemPath = "Window/Deployment";
#endif
        internal static readonly string s_DeploymentWindowAssetsPath = PathUtils.Join(Constants.k_EditorRootPath, "Interface", "UI", "Assets");

        protected DeploymentView m_DeploymentView;
        protected NoConnectionView m_NoConnectionView;
        protected NotSignedInView m_NotSignedInView;
        protected EnvironmentNotSetView m_EnvironmentNotSetView;
        protected UnlinkedProjectView m_UnlinkedProjectView;
        protected MultipleDefinitionsView m_MultipleDefinitionsView;
        protected NoPackagesInstalledView m_NoPackagesInstalledView;
        protected VisualElement m_EmptyListState;
        DeploymentToolbar m_DeploymentToolbar;

        EnvironmentView m_EnvironmentView;
        [NonSerialized]
        bool m_EnvironmentViewInitialized;

        IDeploymentViewModel m_DeploymentViewModel;
        IDeploymentWindowAnalytics m_DeploymentWindowAnalytics;
        IDeploymentWindowStateProvider m_DeploymentWindowStateProvider;
        IDeploymentDefinitionService m_DeploymentDefinitionService;
        IDeploymentSettings m_DeploymentSettings;
        IEnvironmentFetcher m_EnvironmentFetcher;
        ICommandManager m_CommandManager;
        EditorValueTracker<string> m_ProjectIdTracker;

        VisualElement m_CurrentView;

        [MenuItem(k_DeploymentMenuItemPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<DeploymentWindow>();
            window.minSize = new Vector2(250, 50);
            StaticAnalytics.SendOpenedEvent();
        }

        public void Initialize(IDeploymentWindowAnalytics deploymentWindowAnalytics,
            IDeploymentWindowStateProvider deploymentWindowStateProvider,
            IDeploymentDefinitionService deploymentDefinitionService,
            IDeploymentSettings deploymentSettings,
            IEnvironmentFetcher environmentFetcher,
            IDeploymentViewModel deploymentViewModel,
            ICommandManager commandManager)
        {
            m_DeploymentWindowAnalytics = deploymentWindowAnalytics;
            m_DeploymentWindowStateProvider = deploymentWindowStateProvider;
            m_DeploymentDefinitionService = deploymentDefinitionService;
            m_DeploymentSettings = deploymentSettings;
            m_EnvironmentFetcher = environmentFetcher;
            m_DeploymentViewModel = deploymentViewModel;
            m_CommandManager = commandManager;
        }

        protected internal override void LoadGui()
        {
            var fullIconPath = PathUtils.Join(s_DeploymentWindowAssetsPath, k_IconPath);
            titleContent = new GUIContent(k_WindowTitle, LoadThemedIcon(fullIconPath));

            VisualElement root = rootVisualElement;

            // Fetch Required assets
            var deploymentVisualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PathUtils.Join(s_DeploymentWindowAssetsPath, k_DeploymentWindowTemplateName));

            deploymentVisualTree.CloneTree(root);

            AddThemedStyleSheet();

            InitializeViews(root);

            m_ProjectIdTracker = new EditorValueTracker<string>(() => CloudProjectSettings.projectId);
            m_ProjectIdTracker.ValueChanged += async(_, _) =>
            {
                if (string.IsNullOrEmpty(CloudProjectSettings.projectId))
                {
                    return;
                }

                await BindEnvironments();
            };
        }

        void InitializeViews(VisualElement root)
        {
            m_DeploymentView = root.Q<DeploymentView>();
            m_NoConnectionView = root.Q<NoConnectionView>();
            m_NotSignedInView = root.Q<NotSignedInView>();
            m_UnlinkedProjectView = root.Q<UnlinkedProjectView>();
            m_EnvironmentNotSetView = root.Q<EnvironmentNotSetView>();
            m_MultipleDefinitionsView = root.Q<MultipleDefinitionsView>();
            m_NoPackagesInstalledView = root.Q<NoPackagesInstalledView>();
            m_EmptyListState = root.Q<VisualElement>(k_EmptyListStateName);
            m_EnvironmentView = root.Q<EnvironmentView>();
            m_DeploymentToolbar = root.Q<DeploymentToolbar>();

            m_DeploymentToolbar.Bind(m_DeploymentSettings);
            m_DeploymentView.Bind(m_DeploymentViewModel,
                m_DeploymentWindowAnalytics,
                m_DeploymentDefinitionService,
                m_CommandManager);

            m_DeploymentView.AddToClassList(k_HiddenClassName);
            m_NoConnectionView.AddToClassList(k_HiddenClassName);
            m_UnlinkedProjectView.AddToClassList(k_HiddenClassName);
            m_EnvironmentNotSetView.AddToClassList(k_HiddenClassName);
            m_MultipleDefinitionsView.AddToClassList(k_HiddenClassName);
            m_NoPackagesInstalledView.AddToClassList(k_HiddenClassName);
            m_EmptyListState.AddToClassList(k_HiddenClassName);
        }

        async void OnGUI()
        {
            if (!m_Initialized)
            {
                return;
            }

            CheckStateAndSetView();

            await SetupEnvironmentView();
        }

        Task SetupEnvironmentView()
        {
            if (m_EnvironmentFetcher == null
                || m_EnvironmentViewInitialized
                || !m_DeploymentWindowStateProvider.IsInternetConnected()
                || !m_DeploymentWindowStateProvider.IsProjectLinked())
            {
                return Task.CompletedTask;
            }

            m_EnvironmentView.RequestRequery += UpdateEnvironments;
            m_EnvironmentView.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_EnvironmentView.RequestRequery += UpdateEnvironments;
            });
            m_EnvironmentViewInitialized = true;

            return BindEnvironments();
        }

        async void UpdateEnvironments()
        {
            await Sync.SafeAsync(
                m_EnvironmentFetcher.FetchEnvironments,
                task => m_EnvironmentView.UpdateEnvironments(task.Result)
            );
        }

        Task BindEnvironments()
        {
            return Sync.SafeAsync(
                m_EnvironmentFetcher.FetchEnvironments,
                task => m_EnvironmentView.Bind(m_DeploymentSettings, task.Result)
            );
        }

        void CheckStateAndSetView()
        {
            if (m_DeploymentWindowStateProvider == null)
                return;

            if (!m_DeploymentWindowStateProvider.IsInternetConnected())
            {
                SetView(m_NoConnectionView);
            }
            else if (!m_DeploymentWindowStateProvider.IsSignedIn())
            {
                SetView(m_NotSignedInView);
            }
            else if (!m_DeploymentWindowStateProvider.IsProjectLinked())
            {
                SetView(m_UnlinkedProjectView);
            }
            else if (!m_DeploymentWindowStateProvider.IsEnvironmentSet())
            {
                SetView(m_EnvironmentNotSetView);
            }
            else if (m_DeploymentWindowStateProvider.ContainsDuplicateDeploymentDefinitions(out var error))
            {
                m_MultipleDefinitionsView.SetError(error);
                SetView(m_MultipleDefinitionsView);
            }
            else if (!m_DeploymentWindowStateProvider.AreSupportedPackagesInstalled())
            {
                SetView(m_NoPackagesInstalledView);
            }
            else
            {
                SetDeploymentView();
            }
        }

        void SetView(VisualElement view)
        {
            if (m_CurrentView == view)
            {
                return;
            }

            if (m_CurrentView != null)
            {
                m_CurrentView.AddToClassList(k_HiddenClassName);
            }

            m_CurrentView = view;
            m_CurrentView.RemoveFromClassList(k_HiddenClassName);
            m_DeploymentToolbar.DeployEnabled = m_CurrentView == m_DeploymentView;
        }

        /// <summary>
        /// Looks for Icon.png Icon@2x.png d_Icon.png d_Icon@2x.png and picks
        /// the correct one for the currently activated theme
        /// </summary>
        /// <param name="iconPath">Path to the default icon to load</param>
        /// <returns>Texture2D of the icon corresponding to the theme</returns>
        static Texture2D LoadThemedIcon(string iconPath)
        {
            return EditorGUIUtility.FindTexture(iconPath);
        }

        void AddThemedStyleSheet()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                EditorGUIUtility.isProSkin
                ? StyleSheets.Dark
                : StyleSheets.Light);
            rootVisualElement.styleSheets.Add(styleSheet);
        }

        void SetDeploymentView()
        {
            if (!m_DeploymentViewModel.DeploymentItems.Any())
            {
                SetView(m_EmptyListState);
                return;
            }

            SetView(m_DeploymentView);
        }

        void OnDestroy()
        {
            m_ProjectIdTracker.Dispose();
        }

        internal static class StyleSheets
        {
            public const string Light = "Packages/com.unity.services.deployment/Editor/Interface/UI/Assets/DeploymentWindow_Light.uss";
            public const string Dark = "Packages/com.unity.services.deployment/Editor/Interface/UI/Assets/DeploymentWindow_Dark.uss";
        }
    }
}
