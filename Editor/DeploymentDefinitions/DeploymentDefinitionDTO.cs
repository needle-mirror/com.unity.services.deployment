using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    /// <summary>
    /// Serializable representation of a deployment definition (.ddef) file.
    /// </summary>
    public class DeploymentDefinitionDTO
    {
        /// <summary>The name of the deployment definition.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Paths excluded from the deployment definition.</summary>
        [JsonProperty("excludePaths")]
        public List<string> ExcludePaths { get; set; }

        /// <summary>Any other properties found in the file that are not mapped to a field.</summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }
}
