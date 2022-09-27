using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Interface
{
    interface IDeploymentItemViewModel : IDeploymentItem
    {
        string Service { get; }
        IDeploymentItem OriginalItem { get; }
    }
}
