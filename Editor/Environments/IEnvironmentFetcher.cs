using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Services.Deployment.Editor.Environments
{
    interface IEnvironmentFetcher
    {
        Task<Dictionary<string, EnvironmentInfo>> FetchEnvironments();
        Task<EnvironmentInfo> FetchEnvironment(string environmentId);
    }
}
