using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GlobExpressions;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Deployment.Core
{
    static class DeploymentDefinitionServiceExtensions
    {
        public static bool HasDuplicateDeploymentDefinitions(this IDeploymentDefinitionService service, out string duplicationError)
        {
            duplicationError = string.Empty;
            var error = new StringBuilder();

            var duplicateFolders = service.DeploymentDefinitions
                .Select(dDef => Path.GetDirectoryName(dDef.Path))
                .GroupBy(p => p)
                .Where(g => g.Count() > 1)
                .Select(x => x.Key);

            foreach (var path in duplicateFolders)
            {
                error.AppendLine($"Folder <{path}> contains multiple deployment definition files.");
            }

            var duplicateNames = service.DeploymentDefinitions
                .GroupBy(dDef => dDef.Name)
                .Where(g => g.Count() > 1)
                .Select(x => x.Key);

            foreach (var name in duplicateNames)
            {
                error.AppendLine($"Multiple deployment definition files are named <{name}>.");
            }

            if (error.Length > 0)
            {
                duplicationError = error.ToString().TrimEnd();
                return true;
            }

            return false;
        }

        public static bool IsPathExcludedByDeploymentDefinition(
            this IDeploymentDefinitionService service,
            string path,
            IDeploymentDefinition deploymentDefinition)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var normalizedPath = GetNormalizedPath(path);
            var normalizedItemFullPath = GetNormalizedPath(Path.GetFullPath(path));
            var fileName = Path.GetFileName(normalizedPath);
            var parentDir = Directory.GetParent(deploymentDefinition.Path);

            foreach (var excludePath in deploymentDefinition.ExcludePaths)
            {
                try
                {
                    var normalizedExcludePath = GetNormalizedPath(excludePath);

                    if (Glob.IsMatch(normalizedPath, normalizedExcludePath)
                        || Glob.IsMatch(normalizedItemFullPath, normalizedExcludePath)
                        || Glob.IsMatch(fileName, normalizedExcludePath))
                    {
                        return true;
                    }

                    if (parentDir != null)
                    {
                        var normalizedExcludeFullPath = GetNormalizedPath(Path.GetFullPath(Path.Combine(parentDir.FullName, excludePath)));
                        if (Glob.IsMatch(normalizedExcludeFullPath, normalizedItemFullPath))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return false;
        }

        public static IReadOnlyList<string> GetInvalidExcludePaths(this IDeploymentDefinition definition)
        {
            var invalidPaths = new List<string>();
            foreach (var excludePath in definition.ExcludePaths)
            {
                if (string.IsNullOrWhiteSpace(excludePath))
                    continue;
                try
                {
                    Glob.IsMatch("test", excludePath);
                }
                catch (Exception)
                {
                    invalidPaths.Add(excludePath);
                }
            }

            return invalidPaths;
        }

        static string GetNormalizedPath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
