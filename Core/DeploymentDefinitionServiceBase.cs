using System.Collections.Generic;
using System.IO;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Deployment.Core
{
    abstract class DeploymentDefinitionServiceBase : IDeploymentDefinitionService
    {
        public abstract IReadOnlyList<IDeploymentDefinition> DeploymentDefinitions { get; }

        public virtual IDeploymentDefinition DefinitionForPath(string path)
        {
            if (path == null)
            {
                return null;
            }

            path = Path.GetFullPath(path);

            string dirPath = path;
            if (File.Exists(path))
            {
                dirPath = Path.GetDirectoryName(path) ?? "";
            }

            var bestPath = string.Empty;

            var dirPathWithSeparator = dirPath + Path.DirectorySeparatorChar;

            IDeploymentDefinition bestDefinition = null;
            foreach (var definition in DeploymentDefinitions)
            {
                var definitionFullPath = Path.GetFullPath(definition.Path);
                var definitionRootDir = Path.GetDirectoryName(definitionFullPath);
                if (!string.IsNullOrEmpty(definitionRootDir)
                    && dirPathWithSeparator.StartsWith(definitionRootDir + Path.DirectorySeparatorChar, System.StringComparison.Ordinal)
                    && definitionRootDir.Length > bestPath.Length)
                {
                    bestDefinition = definition;
                    bestPath = definitionRootDir;
                }
            }

            return bestDefinition;
        }
    }
}
