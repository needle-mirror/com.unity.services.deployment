using Unity.Services.Core.Editor;

namespace Unity.Services.Deployment.Editor.Settings
{
    struct DeploymentIdentifier : IEditorGameServiceIdentifier
    {
        public string GetKey()
        {
            return "Deployment";
        }
    }
}
