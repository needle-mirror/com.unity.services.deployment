using System;
using System.Linq;
using Unity.Services.Deployment.Editor.DeploymentDefinitions;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Deployment.Editor.State
{
    sealed class DeploymentWindowStateProvider : IDeploymentWindowStateProvider
    {
        readonly IEnvironmentProvider m_EnvironmentProvider;
        readonly IDeploymentDefinitionService m_DeploymentDefinitionService;

        bool m_ContainsDuplicateDefinitions;
        string m_DuplicateDefinitionsError;

        public DeploymentWindowStateProvider(IEnvironmentProvider environmentProvider, IDeploymentDefinitionService deploymentDefinitionService)
        {
            m_EnvironmentProvider = environmentProvider;
            m_DeploymentDefinitionService = deploymentDefinitionService;

            if (m_DeploymentDefinitionService.DeploymentDefinitions.Any())
            {
                ValidateDeploymentDefinitionService();
            }

            m_DeploymentDefinitionService.DeploymentDefinitions.CollectionChanged += OnDeploymentDefinitionsCollectionChanged;
        }

        public bool IsInternetConnected()
        {
            return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        }

        public bool IsEnvironmentSet()
        {
            return !string.IsNullOrEmpty(m_EnvironmentProvider.Current);
        }

        public bool IsProjectLinked()
        {
            return !(string.IsNullOrEmpty(CloudProjectSettings.organizationId) ||
                string.IsNullOrEmpty(CloudProjectSettings.organizationName) ||
                string.IsNullOrEmpty(CloudProjectSettings.projectId) ||
                string.IsNullOrEmpty(CloudProjectSettings.projectName));
        }

        public bool IsSignedIn()
        {
            const string k_UserNameAnonymous = "anonymous";
            bool hasUserId = string.IsNullOrEmpty(CloudProjectSettings.userId);
            bool isAnonymous = string.IsNullOrEmpty(CloudProjectSettings.userName)
                || CloudProjectSettings.userName.Equals(k_UserNameAnonymous, StringComparison.InvariantCultureIgnoreCase);
            return !hasUserId
                && !isAnonymous;
        }

        public bool ContainsDuplicateDeploymentDefinitions(out string error)
        {
            error = m_DuplicateDefinitionsError;
            return m_ContainsDuplicateDefinitions;
        }

        public bool AreSupportedPackagesInstalled()
        {
            return Deployments.Instance.DeploymentProviders.Any();
        }

        public void Dispose()
        {
            m_DeploymentDefinitionService.DeploymentDefinitions.CollectionChanged -= OnDeploymentDefinitionsCollectionChanged;
        }

        void OnDeploymentDefinitionsCollectionChanged(object sender, EventArgs args)
        {
            ValidateDeploymentDefinitionService();
        }

        void ValidateDeploymentDefinitionService()
        {
            m_ContainsDuplicateDefinitions = false;
            m_DuplicateDefinitionsError = string.Empty;

            if (m_DeploymentDefinitionService.HasDuplicateDeploymentDefinitions(out var error))
            {
                m_ContainsDuplicateDefinitions = true;
                m_DuplicateDefinitionsError = error;
            }
        }
    }
}
