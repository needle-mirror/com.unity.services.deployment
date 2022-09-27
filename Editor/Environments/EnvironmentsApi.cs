using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.DeploymentWindow.Environments.Client.Apis.Default;
using Unity.Services.DeploymentWindow.Environments.Client.Default;

namespace Unity.Services.Deployment.Editor.Environments
{
    class EnvironmentsApi
    {
        readonly string m_ProjectId;
        readonly IDefaultApiClient m_Client;

        public EnvironmentsApi(string projectId, IDefaultApiClient client)
        {
            m_ProjectId = projectId;
            m_Client = client;
        }

        public async Task<List<EnvironmentInfo>> GetEnvironments()
        {
            var request = new UnityGetEnvironmentsRequest(m_ProjectId);
            var response = await m_Client.UnityGetEnvironmentsAsync(request);
            return response.Result.Results.Select(unityEnvironment => new EnvironmentInfo(unityEnvironment)).ToList();
        }

        public async Task<EnvironmentInfo> GetEnvironment(string environmentId)
        {
            var request = new UnityGetEnvironmentRequest(m_ProjectId, environmentId);
            var response = await m_Client.UnityGetEnvironmentAsync(request);
            return new EnvironmentInfo(response.Result);
        }
    }
}
