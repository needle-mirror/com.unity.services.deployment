using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions.UI
{
    class CreateDeploymentDefinition : EndNameEditAction
    {
        const string k_DefaultName = "new_deployment_definition";
        static readonly string k_MonoDefinitionPath = Path.Combine(Constants.k_EditorRootPath, "DeploymentDefinitions/DeploymentDefinition.cs");

        [MenuItem("Assets/Create/Deployment Definition", false, 81)]
        public static void CreateDeploymentDefinitionFile()
        {
            var filePath = k_DefaultName + DeploymentDefinitionResources.FileExtension;
            var icon = DeploymentDefinitionResources.Icon;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                CreateInstance<CreateDeploymentDefinition>(),
                filePath,
                icon,
                null);
        }

        [InitializeOnLoadMethod]
        static void SetMonoDefinitionIcon()
        {
            var monoImporter = (MonoImporter)AssetImporter.GetAtPath(k_MonoDefinitionPath);
            var monoScript = monoImporter.GetScript();
            EditorGUIUtility.SetIconForObject(monoScript,  DeploymentDefinitionResources.Icon);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var definition = CreateInstance<DeploymentDefinition>();

            definition.Name = Path.GetFileNameWithoutExtension(pathName);

            File.WriteAllText(pathName, definition.ToJson());
            AssetDatabase.Refresh();
        }
    }
}
