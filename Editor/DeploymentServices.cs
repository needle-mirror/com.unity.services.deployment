using Unity.Services.Deployment.Editor.Analytics;
using Unity.Services.Deployment.Editor.Analytics.Environment;
using Unity.Services.Deployment.Editor.Commands;
using Unity.Services.Deployment.Editor.Configuration;
using Unity.Services.Deployment.Editor.DeploymentDefinitions;
using Unity.Services.Deployment.Editor.Environments;
using Unity.Services.Deployment.Editor.Environments.UI;
using Unity.Services.Deployment.Editor.Interface;
using Unity.Services.Deployment.Editor.Interface.UI.Components;
using Unity.Services.Deployment.Editor.Interface.UI.Serialization;
using Unity.Services.Deployment.Editor.IO;
using Unity.Services.Deployment.Editor.JsonUtils;
using Unity.Services.Deployment.Editor.PlayMode;
using Unity.Services.Deployment.Editor.Shared.Assets;
using Unity.Services.Deployment.Editor.Shared.Clients;
using Unity.Services.Deployment.Editor.Shared.DependencyInversion;
using Unity.Services.Deployment.Editor.Shared.UI;
using Unity.Services.Deployment.Editor.State;
using Unity.Services.Deployment.Editor.Tracking;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
using UnityEngine;
using AccessTokens = Unity.Services.Deployment.Editor.Environments.Authentication.AccessTokens;

namespace Unity.Services.Deployment.Editor
{
    class DeploymentServices : AbstractRuntimeServices<DeploymentServices>
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Instance.Initialize(new ServiceCollection());
            Deployments.Instance.EnvironmentProvider = Instance.GetService<IEnvironmentProvider>();
            StaticAnalytics.RegisterEvents();
        }

        public DeploymentServices()
        {
        }

        internal override void Register(ServiceCollection collection)
        {
            collection.Register(_ => new AccessTokens());
            collection.Register(_ => Debug.unityLogger);

            collection.RegisterSingleton(Factories.Default<IDeploymentDefinitionService, DeploymentDefinitionService>);

            collection.Register(Factories.Default<IEditorEvents, EditorEvents>);
            collection.Register(Factories.Default<IPlayModeInterrupt, PlayModeInterrupt>);
            collection.RegisterStartupSingleton(Factories.Default<DeployOnPlay>);

            collection.Register(Factories.Default<IDeploymentWindowAnalytics, DeploymentWindowAnalytics>);
            collection.Register(Factories.Default<IDeployOnPlayAnalytics, DeployOnPlayAnalytics>);
            collection.Register(Factories.Default<IProjectPreferences, ProjectPreferences>);
            collection.RegisterSingleton(Factories.Default<IDeploymentSettings, DeploymentSettings>);

            collection.Register(Factories.Default<ICurrentTime, CurrentTime>);
            collection.Register(Factories.Default<IAccessTokens, AccessTokens>);
            collection.RegisterSingleton(Factories.Default<IGatewayTokenProvider, GatewayTokenProvider>);

            collection.Register(Factories.Default<IEnvironmentAnalytics, EnvironmentAnalytics>);
            collection.Register(Factories.Default<IEnvironmentFetcher, EnvironmentsApiFactory>);
            collection.Register(Factories.Default<IProjectInfo, ProjectInfo>);

            collection.Register(Factories.Default<EnvironmentDropdown>);
            collection.Register(Factories.Default<IEnvironmentValidator, EnvironmentValidator>);
            collection.RegisterSingleton(Factories.Default<IEnvironmentProvider, DeploymentSettings>);

            collection.Register(Factories.Default<IDeploymentViewModel, DeploymentViewModel>);
            collection.Register(_ => Deployments.Instance.DeploymentProviders);
            collection.Register(_ => Deployments.Instance);
            collection.Register(Factories.Default<IDeploymentWindowStateProvider, DeploymentWindowStateProvider>);
            collection.Register(Factories.Default<IDeploymentItemTracker, DeploymentItemTracker>);
            collection.Register(Factories.Default<INotifications, Notifications>);

            collection.RegisterSingleton(Factories.Default<ICommandManager, DeploymentCommandManager>);
            collection.Register(Factories.Default<SelectInProjectWindowCommand>);
            collection.Register(Factories.Default<IKeyboardShortcuts, KeyboardShortcuts>);
            collection.Register(Factories.Default<IKeyboardSelectionLogic, KeyboardSelectionLogic>);
            collection.Register(Factories.Default<ISerializationManager, SerializationManager>);
            collection.Register(Factories.Default<ISerializableComponentFetcher, UiTreeSerializableComponentFetcher>);
            collection.Register(Factories.Default<IJsonConverter, NewtonsoftJsonConverter>);
            collection.Register(Factories.Default<IDeployOnPlayItemRetriever, DeployOnPlayItemRetriever>);

            collection.RegisterStartupSingleton(Factories.Default<ItemStatusTracker>);
            collection.Register(Factories.Default<IFileTracker, FileTracker>);
            collection.Register(Factories.Default<AssetPostprocessorProxy>);
        }
    }
}
