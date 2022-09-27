using Unity.Services.Core.Editor;

namespace Unity.Services.Deployment.Editor.Settings
{
    class DeploymentService : IEditorGameService
    {
        public string Name { get; } = "DeploymentService";
        public IEditorGameServiceIdentifier Identifier { get; } = new DeploymentIdentifier();
        public bool RequiresCoppaCompliance { get; } = false;
        public bool HasDashboard { get; } = false;
        public string GetFormattedDashboardUrl()
        {
            return null;
        }

        public IEditorGameServiceEnabler Enabler { get; } = null;
    }
}
