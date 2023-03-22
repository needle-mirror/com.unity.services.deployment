using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Shared.Clients;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Environments
{
    class EnvironmentValidator : IEnvironmentValidator
    {
        readonly IEnvironmentProvider m_EnvironmentProvider;
        readonly IGatewayTokenProvider m_TokenProvider;
        readonly IEnvironmentFetcher m_EnvironmentFetcher;
        readonly IProjectInfo m_ProjectInfo;

        public EnvironmentValidator(IProjectInfo projectInfo, IEnvironmentFetcher fetcher, IEnvironmentProvider environmentProvider, IGatewayTokenProvider tokenProvider)
        {
            m_EnvironmentProvider = environmentProvider;
            m_TokenProvider = tokenProvider;
            m_EnvironmentFetcher = fetcher;
            m_ProjectInfo = projectInfo;
        }

        public async Task<ValidationResult> ValidateEnvironmentAsync()
        {
            var result = new ValidationResult();
            var projectId = m_ProjectInfo.ProjectId;
            var gatewayToken = await m_TokenProvider.FetchGatewayToken();
            var environmentId = m_EnvironmentProvider.Current;

            if (string.IsNullOrEmpty(environmentId))
            {
                result.Error = "Environment is not set!. Please set it through the Environment Selector at Edit -> Project Settings -> Deployment";
            }

            if (string.IsNullOrEmpty(projectId))
            {
                result.Error = "Project is not linked! Please make sure that you have properly linked a project.";
            }

            if (string.IsNullOrEmpty(gatewayToken))
            {
                result.Error = "Unable to get login credentials! Please make sure that you have properly linked a project.";
            }

            if (!await EnvironmentExistsInProjectAsync(environmentId))
            {
                result.Error = "Environment does not exist in the current project!";
            }

            return result;
        }

        async Task<bool> EnvironmentExistsInProjectAsync(string environmentId)
        {
            try
            {
                var env = await m_EnvironmentFetcher.FetchEnvironment(environmentId);
                return env.HasValue;
            }
            catch
            {
                return false;
            }
        }
    }
}
