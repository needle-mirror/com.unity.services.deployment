using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    sealed class DeploymentDefinitionService : IDeploymentDefinitionService, IDisposable
    {
        internal const string DefaultName = "Default";
        internal const string DefaultPath = "DefaultPath";

        public DeploymentDefinition DefaultDefinition { get; }
        public ObservableCollection<DeploymentDefinition> DeploymentDefinitions => m_DeploymentDefinitions;
        readonly DeploymentDefinitionCollection m_DeploymentDefinitions = new DeploymentDefinitionCollection();

        public DeploymentDefinitionService()
        {
            DefaultDefinition = ScriptableObject.CreateInstance<DeploymentDefinition>();
            DefaultDefinition.Name = DefaultName;
            DefaultDefinition.Path = DefaultPath;
        }

        public void Dispose()
        {
            Object.DestroyImmediate(DefaultDefinition);
            m_DeploymentDefinitions.Dispose();
        }
    }
}
