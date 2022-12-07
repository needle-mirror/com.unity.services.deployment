using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Services.Deployment.Editor.Shared.EditorUtils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions.UI
{
    [CustomEditor(typeof(DeploymentDefinition))]
    [CanEditMultipleObjects]
    class DeploymentDefinitionEditor : UnityEditor.Editor
    {
        static readonly string k_UxmlPath = Path.Combine(Constants.k_EditorRootPath, "DeploymentDefinitions/UI/Assets/DeploymentDefinitionEditorUI.uxml");

        IEnumerable<DeploymentDefinition> Targets => serializedObject.targetObjects.Cast<DeploymentDefinition>();

        ApplyRevertChangeTracker<DeploymentDefinition> m_ChangeTracker;
        VisualElement m_ApplyFooter;

        public override VisualElement CreateInspectorGUI()
        {
            DisableReadonlyFlags();
            m_ChangeTracker = new ApplyRevertChangeTracker<DeploymentDefinition>(serializedObject);

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            var rootElement = new VisualElement();
            uxmlAsset.CloneTree(rootElement);

            BindControls(rootElement);

            return rootElement;
        }

        void BindControls(VisualElement rootElement)
        {
            rootElement.Bind(m_ChangeTracker.SerializedObject);

            m_ApplyFooter = rootElement.Q<VisualElement>(UxmlNames.ApplyFooter);

            rootElement.Q<Button>(UxmlNames.Apply).clicked += ApplyChanges;
            rootElement.Q<Button>(UxmlNames.Revert).clicked += RevertChanges;

            foreach (var property in rootElement.Query<PropertyField>().Build())
            {
                property.RegisterValueChangeCallback(_ => UpdateApplyRevertEnabled());
            }

            UpdateApplyRevertEnabled();
        }

        void ApplyChanges()
        {
            m_ChangeTracker.Apply();
            foreach (var definition in Targets)
            {
                definition.SaveChanges();
            }
            UpdateApplyRevertEnabled();
            AssetDatabase.Refresh();
        }

        void RevertChanges()
        {
            m_ChangeTracker.Reset();
            UpdateApplyRevertEnabled();
        }

        void UpdateApplyRevertEnabled()
        {
            m_ApplyFooter.SetEnabled(m_ChangeTracker.IsDirty());
        }

        void DisableReadonlyFlags()
        {
            serializedObject.targetObject.hideFlags = HideFlags.None;
        }

        static class UxmlNames
        {
            public const string Apply = "Apply";
            public const string Revert = "Revert";
            public const string ApplyFooter = "Apply Footer";
        }
    }
}
