using System.Collections.ObjectModel;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    interface IDeploymentDefinitionService
    {
        DeploymentDefinition DefaultDefinition { get; }
        ObservableCollection<DeploymentDefinition> DeploymentDefinitions { get; }
    }
}
