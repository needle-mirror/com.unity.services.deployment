using UnityEditor;

namespace Unity.Services.Deployment.Editor.Environments
{
    class ProjectIdProvider : IProjectIdProvider
    {
        public string Id => CloudProjectSettings.projectId;

        public ProjectIdProvider()
        {
        }
    }
}
