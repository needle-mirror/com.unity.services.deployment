using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Shared.Collections;

namespace Unity.Services.Deployment.Editor.Interface
{
    interface IDeploymentViewModel
    {
        IReadOnlyObservable<IDeploymentItemViewModel> DeploymentItems { get; }
        Task DeployItemsAsync(IEnumerable<IDeploymentItemViewModel> items);
    }
}
