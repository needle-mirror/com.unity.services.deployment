#if UNITY_PIPELINE_AVAILABLE && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using Unity.Services.Deployment.Cli;
using Unity.Services.Deployment.Editor.Commands;
using Unity.Services.Deployment.Editor.DeploymentDefinitions;
using Unity.Services.Deployment.Editor.Interface;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Deployment.Editor
{
    static class DeploymentEditorCliCommands
    {
        [CliGroupCommand(DeploymentCliConstants.Group, "Get the deployable items across all registered providers")]
        public static List<DeployableItemDto> ListItems()
        {
            return WithWindow(window => window
                .GetAllDeploymentItems(includeDeploymentDefinitions: false)
                .Select(DeployableItemDto.From)
                .ToList());
        }

        [CliGroupCommand(DeploymentCliConstants.Group, "Get the deployment definitions (.ddef files) in the project")]
        public static List<DeploymentDefinitionDto> ListDefinitions()
        {
            return WithWindow(window => window
                .GetDeploymentDefinitions()
                .Select(DeploymentDefinitionDto.From)
                .ToList());
        }

        [CliGroupCommand(DeploymentCliConstants.Group, "Deploy the items at the given paths (comma-separated) through the Deployment Window API (dependency resolution + environment validation). Deploys to the active Unity Services environment — set it first with services.environment set_active_by_name")]
        public static Task<DeployResultDto> DeployItems(
            [CliArg("paths", "Comma-separated deployable item paths", Required = true)] string paths)
        {
            var requested = (paths ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            return WithWindowAsync(async window =>
            {
                var resolved = window.GetFromFiles(requested);
                var notFound = requested.Where((_, i) => resolved[i] == null).ToList();
                var found = resolved.Where(item => item != null).ToList();

                if (found.Count == 0)
                    throw new ArgumentException($"No deployable items matched the given path(s): {string.Join(", ", notFound)}");

                var result = await window.Deploy(found, CancellationToken.None);

                return new DeployResultDto
                {
                    Requested = requested.Count,
                    Deployed = result.Deployed.Select(DeployableItemDto.From).ToList(),
                    NotFound = notFound
                };
            });
        }

        [CliGroupCommand(DeploymentCliConstants.Group, "Get the commands available for the deployable item at the given path")]
        public static List<ItemCommandDto> ListItemCommands(
            [CliArg("path", "The deployable item path", Required = true)] string path)
        {
            return WithWindow(window =>
            {
                var single = new object[] { ResolveItem(window, path) };
                return CommandManagerOf(window)
                    .GetCommandsForObjects(single)
                    .Select(c => new ItemCommandDto
                    {
                        Name = c.Name,
                        IsVisible = c.IsVisible(single),
                        IsEnabled = c.IsEnabled(single)
                    })
                    .ToList();
            });
        }

        [CliGroupCommand(DeploymentCliConstants.Group, "Execute the named command on the deployable item at the given path")]
        public static Task<ItemCommandResultDto> ExecuteItemCommand(
            [CliArg("path", "The deployable item path", Required = true)] string path,
            [CliArg("command", "The command name (see services.deployment list_item_commands)", Required = true)] string command)
        {
            return WithWindowAsync(async window =>
            {
                var item = ResolveItem(window, path);
                var single = new object[] { item };

                var commands = CommandManagerOf(window).GetCommandsForObjects(single);
                var match = commands.FirstOrDefault(c => string.Equals(c.Name, command, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    var available = string.Join(", ", commands.Select(c => c.Name));
                    throw new ArgumentException($"'{command}' is not a command for item '{path}'. Available: {available}");
                }

                await match.Execute(single);

                return new ItemCommandResultDto
                {
                    Name = item.Name,
                    Path = item.Path,
                    Command = match.Name,
                    Status = item.Status.Message
                };
            });
        }

        [CliGroupCommand(DeploymentCliConstants.Group, "Create a Deployment Definition (.ddef) asset seeded with default content")]
        public static CreatedConfigDto CreateDefinition(
            [CliArg("path", "Project-relative asset path (the Assets/ prefix and .ddef extension are optional). Defaults to Assets/new_deployment_definition.ddef")] string path = null,
            [CliArg("name", "The definition name written into the file (optional, defaults to the file name without extension)")] string name = null,
            [CliArg("exclude-paths", "Comma-separated glob paths to exclude from the definition (optional)")] string excludePaths = null)
        {
            var assetPath = PrepareConfigPath(path, "new_deployment_definition", DeploymentDefinitionResources.FileExtension);

            var definition = ScriptableObject.CreateInstance<DeploymentDefinition>();
            definition.Name = string.IsNullOrEmpty(name)
                ? Path.GetFileNameWithoutExtension(assetPath)
                : name;

            if (!string.IsNullOrEmpty(excludePaths))
            {
                foreach (var excludePath in excludePaths.Split(','))
                {
                    var trimmed = excludePath.Trim();
                    if (trimmed.Length > 0)
                        definition.ExcludePaths.Add(trimmed);
                }
            }

            File.WriteAllText(Path.GetFullPath(assetPath), definition.ToJson());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return new CreatedConfigDto { Type = nameof(DeploymentDefinition), Path = assetPath };
        }

        // Normalize a caller-supplied path into a unique project-relative path with the
        // given config extension, creating any intermediate folders. Forgiving about the
        // Assets/ prefix, separators and a missing extension.
        static string PrepareConfigPath(string path, string defaultName, string extension)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = defaultName;

            path = path.Replace('\\', '/').Trim().TrimStart('/');
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                path = "Assets/" + path;
            }

            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                path += extension;

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder))
                CreateFolderRecursive(folder);

            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        static void CreateFolderRecursive(string assetsFolder)
        {
            if (AssetDatabase.IsValidFolder(assetsFolder))
                return;

            var parent = Path.GetDirectoryName(assetsFolder)?.Replace('\\', '/');
            var name = Path.GetFileName(assetsFolder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new ArgumentException($"Invalid asset folder path '{assetsFolder}'.");

            if (!AssetDatabase.IsValidFolder(parent))
                CreateFolderRecursive(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        static IDeploymentItem ResolveItem(IDeploymentWindow window, string path)
        {
            var item = window.GetFromFiles(new[] { path }).FirstOrDefault();
            if (item == null)
                throw new ArgumentException($"No deployable item found at path '{path}'.");
            return item;
        }

        static ICommandManager CommandManagerOf(IDeploymentWindow window)
            => ((IDeploymentViewModel)window).CommandManager;

        static T WithWindow<T>(Func<IDeploymentWindow, T> body)
        {
            var window = HeadlessDeploymentWindow.Resolve();
            try
            {
                return body(window);
            }
            finally
            {
                (window as IDisposable)?.Dispose();
            }
        }

        static async Task<T> WithWindowAsync<T>(Func<IDeploymentWindow, Task<T>> body)
        {
            var window = HeadlessDeploymentWindow.Resolve();
            try
            {
                return await body(window);
            }
            finally
            {
                (window as IDisposable)?.Dispose();
            }
        }

        /// <summary>Serializable view of a deployable item.</summary>
        public class DeployableItemDto
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }

            public static DeployableItemDto From(IDeploymentItem item)
            {
                return new DeployableItemDto
                {
                    Name = item.Name,
                    Path = item.Path,
                    Type = (item as ITypedItem)?.Type,
                    Status = item.Status.Message
                };
            }
        }

        /// <summary>Serializable view of a .ddef deployment definition.</summary>
        public class DeploymentDefinitionDto
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public List<string> ExcludePaths { get; set; }
            public List<string> ItemPaths { get; set; }

            public static DeploymentDefinitionDto From(IDeploymentDefinitionItem ddef)
            {
                return new DeploymentDefinitionDto
                {
                    Name = ddef.Name,
                    Path = ddef.Path,
                    ExcludePaths = ddef.ExcludePaths?.ToList() ?? new List<string>(),
                    ItemPaths = ddef.Children?.Select(c => c.Path).ToList() ?? new List<string>()
                };
            }
        }

        /// <summary>Result of a services.deployment deploy_items call.</summary>
        public class DeployResultDto
        {
            public int Requested { get; set; }
            public List<DeployableItemDto> Deployed { get; set; }
            public List<string> NotFound { get; set; }
        }

        /// <summary>A command available on an item, with its visibility/enabled state.</summary>
        public class ItemCommandDto
        {
            public string Name { get; set; }
            public bool IsVisible { get; set; }
            public bool IsEnabled { get; set; }
        }

        /// <summary>Result of executing a command on an item.</summary>
        public class ItemCommandResultDto
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Command { get; set; }
            public string Status { get; set; }
        }

        /// <summary>Result of creating a config-as-code deployment asset.</summary>
        public class CreatedConfigDto
        {
            public string Type { get; set; }
            public string Path { get; set; }
        }
    }
}
#endif
