using System.IO;
using Newtonsoft.Json;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    [ScriptedImporter(1, DeploymentDefinitionResources.FileExtension)]
    class DeploymentDefinitionImporter : ScriptedImporter
    {
        public void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var fileContent = File.ReadAllText(ctx.assetPath);
            var definition = ScriptableObject.CreateInstance<DeploymentDefinition>();

            try
            {
                definition.FromJson(fileContent);
            }
            catch (JsonReaderException e)
            {
                Debug.LogError($"[Deployment Definition] '{ctx.assetPath}': {e.Message}", this);
            }

            ctx.AddObjectToAsset("MainAsset", definition);
            ctx.SetMainObject(definition);
        }
    }
}
