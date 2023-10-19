using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GlobExpressions;
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

            var fullPath = path;
            if (!Path.IsPathFullyQualified(fullPath))
            {
                fullPath = Path.GetFullPath(fullPath);
            }

            var dirPath = Path.GetDirectoryName(fullPath);

            var bestPath = string.Empty;

            IDeploymentDefinition bestDefinition = null;
            foreach (var definition in DeploymentDefinitions)
            {
                var definitionRootDir = Directory.GetParent(definition.Path)?.FullName;
                if (definitionRootDir != null
                    && dirPath.Contains(definitionRootDir)
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
