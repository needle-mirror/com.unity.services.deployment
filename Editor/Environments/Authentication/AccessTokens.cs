using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Shared.Clients;
using UnityEditor;

namespace Unity.Services.Deployment.Editor.Environments.Authentication
{
    class AccessTokens : IAccessTokens
    {
        static readonly Core.Editor.AccessTokens k_AccessTokens = new Core.Editor.AccessTokens();

        public string GenesisAccessToken => CloudProjectSettings.accessToken;

        public Task<string> GetServicesGatewayTokenAsync()
        {
            return k_AccessTokens.GetServicesGatewayTokenAsync();
        }
    }
}
