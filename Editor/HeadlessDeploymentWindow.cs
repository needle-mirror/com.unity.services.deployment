using Unity.Services.Deployment.Editor.Interface;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor
{
    static class HeadlessDeploymentWindow
    {
        internal static IDeploymentWindow Resolve()
        {
            return (IDeploymentWindow)DeploymentServices.Instance.GetService<IDeploymentViewModel>();
        }
    }
}
