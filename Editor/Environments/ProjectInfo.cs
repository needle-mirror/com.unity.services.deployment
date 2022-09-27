using UnityEditor;

namespace Unity.Services.Deployment.Editor.Environments
{
    class ProjectInfo : IProjectInfo
    {
        public string ProjectId => CloudProjectSettings.projectId;
        public string ProjectName => CloudProjectSettings.projectName;
    }
}
