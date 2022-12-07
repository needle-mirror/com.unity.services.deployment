// WARNING: Auto generated code. Modifications will be lost!
using System.Threading.Tasks;

namespace Unity.Services.Deployment.Editor.Shared.Clients
{
    interface IGatewayTokenProvider
    {
        public Task<string> FetchGatewayToken();
        public bool IsStaging();
    }
}
