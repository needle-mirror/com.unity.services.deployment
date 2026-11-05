using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Unity.Services.Deployment.Core.Model
{
    internal interface IDeploymentDefinition
    {
        string Name { get; set; }
        string Path { get; set; }
        ObservableCollection<string> ExcludePaths { get; }
        IReadOnlyDictionary<string, object> AdditionalProperties { get; }
    }
}
