using System;
using Newtonsoft.Json;
using Unity.Services.DeploymentWindow.Environments.Client.Models;

namespace Unity.Services.Deployment.Editor.Environments
{
    struct EnvironmentInfo
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("id")]
        public Guid Id;
        [JsonProperty("isDefault")]
        public bool IsDefault;

        public EnvironmentInfo(string name, Guid id, bool isDefault)
        {
            Name = name;
            Id = id;
            IsDefault = isDefault;
        }

        public EnvironmentInfo(UnityEnvironmentV1 unityEnvironment)
        {
            Name = unityEnvironment.Name;
            Id = unityEnvironment.Id;
            IsDefault = unityEnvironment.IsDefault;
        }
    }
}
