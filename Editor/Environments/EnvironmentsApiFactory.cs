using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Shared.Clients;
using Unity.Services.DeploymentWindow.Environments.Client.Apis.Default;
using Unity.Services.DeploymentWindow.Environments.Client.Http;
using UnityEditor;

namespace Unity.Services.Deployment.Editor.Environments
{
    class EnvironmentsApiFactory : IEnvironmentFetcher
    {
        const string k_StagingUrl = "https://staging.services.unity.com";

        readonly IGatewayTokenProvider m_TokenProvider;

        public EnvironmentsApiFactory(IGatewayTokenProvider gatewayTokenProvider)
        {
            m_TokenProvider = gatewayTokenProvider;
        }

        public async Task<List<EnvironmentInfo>> FetchEnvironments()
        {
            var environmentsApi = await Build();

            if (environmentsApi == null)
            {
                return null;
            }

            return await environmentsApi.GetEnvironments();
        }

        public async Task<EnvironmentInfo?> FetchEnvironment(string environmentId)
        {
            var environmentsApi = await Build();

            if (environmentsApi == null)
            {
                return null;
            }

            return await environmentsApi.GetEnvironment(environmentId);
        }

        async Task<EnvironmentsApi> Build()
        {
            var projectId = CloudProjectSettings.projectId;
            var gatewayToken = await m_TokenProvider.FetchGatewayToken();

            if (gatewayToken == null)
            {
                return null;
            }

            string baseUrl = null;
            if (CloudEnvironmentConfigProvider.IsStaging())
            {
                baseUrl = k_StagingUrl;
            }

            var headers = new AdminApiHeaders<EnvironmentsApiFactory>(gatewayToken);
            var configuration = new DeploymentWindow.Environments.Client.Configuration(baseUrl, null, null, headers.ToDictionary());

            return new EnvironmentsApi(projectId, new DefaultApiClient(new HttpClient(), configuration));
        }
    }
}
