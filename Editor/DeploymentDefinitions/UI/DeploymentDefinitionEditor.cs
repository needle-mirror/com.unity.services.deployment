using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Services.Deployment.Core;
using Unity.Services.Deployment.Editor.Shared.Analytics;
using Unity.Services.Deployment.Editor.Shared.EditorUtils;
using Unity.Services.Deployment.Editor.Shared.UI.DeploymentConfigInspectorFooter;
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
        ListView m_ExcludePathsListView;
        HelpBox m_ExcludePathsErrorBox;
        VisualElement m_ApplyFooter;
        readonly Dictionary<TextField, int> m_ExcludePathsBindingMap = new();

        public override VisualElement CreateInspectorGUI()
        {
            DisableReadonlyFlags();
            m_ChangeTracker = new ApplyRevertChangeTracker<DeploymentDefinition>(serializedObject);

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            var rootElement = new VisualElement();
            uxmlAsset.CloneTree(rootElement);

            BindControls(rootElement);
            SetupConfigFooter(rootElement);

            return rootElement;
        }

        void SetupConfigFooter(VisualElement rootElement)
        {
            var deploymentConfigInspectorFooter = rootElement.Q<DeploymentConfigInspectorFooter>();
            deploymentConfigInspectorFooter.BindGUI(
                AssetDatabase.GetAssetPath(target),
                DeploymentServices.Instance.GetService<ICommonAnalytics>(),
                "deployment");
        }

        void BindControls(VisualElement rootElement)
        {
            rootElement.Bind(m_ChangeTracker.SerializedObject);

            m_ExcludePathsErrorBox = rootElement.Q<HelpBox>(UxmlNames.ExcludePathsError);

            BindApplyFooter(rootElement);
            BindExcludePaths(rootElement);

            foreach (var property in rootElement.Query<PropertyField>().Build())
            {
                property.RegisterValueChangeCallback(_ => UpdateApplyRevertEnabled());
            }

            UpdateApplyRevertEnabled();
        }

        void BindExcludePaths(VisualElement rootElement)
        {
            m_ExcludePathsListView = rootElement.Q<ListView>(UxmlNames.ExcludePaths);
            m_ExcludePathsListView.bindItem = BindExcludePathsItem;
            m_ExcludePathsListView.makeItem = MakeItem;
            var ddef = (DeploymentDefinition)m_ChangeTracker.SerializedObject.targetObjects[0];
            m_ExcludePathsListView.itemsSource = ddef.ExcludePaths;
            m_ExcludePathsListView.itemsRemoved += _ => UpdateOnNextFrame();

            ValidateExcludePaths();

            void UpdateOnNextFrame()
            {
                Sync.RunNextUpdateOnMain(() =>
                {
                    ValidateExcludePaths();
                    UpdateApplyRevertEnabled();
                });
            }
        }


        static VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.Add(new TextField { isDelayed = true });
            return container;
        }

        void BindExcludePathsItem(VisualElement visualElement, int index)
        {
            var textField = visualElement.Q<TextField>();
            m_ExcludePathsBindingMap[textField] = index;
            textField.UnregisterValueChangedCallback(OnExcludePathValueChanged);
            var ddef = (DeploymentDefinition)m_ChangeTracker.SerializedObject.targetObjects[0];
            textField.value = ddef.ExcludePaths[index];
            textField.RegisterValueChangedCallback(OnExcludePathValueChanged);
        }

        void OnExcludePathValueChanged(ChangeEvent<string> changeEvent)
        {
            var ddef = (DeploymentDefinition)m_ChangeTracker.SerializedObject.targetObjects[0];
            ddef.ExcludePaths[m_ExcludePathsBindingMap[(TextField)changeEvent.target]] = changeEvent.newValue;
            ValidateExcludePaths();
            UpdateApplyRevertEnabled();
        }


        void ValidateExcludePaths()
        {
            var ddef = (DeploymentDefinition)m_ChangeTracker.SerializedObject.targetObjects[0];
            var invalidPaths = ddef.GetInvalidExcludePaths();

            if (invalidPaths.Count > 0)
            {
                m_ExcludePathsErrorBox.text = "Invalid patterns, Ignored: " + string.Join(", ", invalidPaths);
                m_ExcludePathsErrorBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_ExcludePathsErrorBox.text = string.Empty;
                m_ExcludePathsErrorBox.style.display = DisplayStyle.None;
            }
        }

        void BindApplyFooter(VisualElement rootElement)
        {
            m_ApplyFooter = rootElement.Q<VisualElement>(UxmlNames.ApplyFooter);

            rootElement.Q<Button>(UxmlNames.Apply).clicked += ApplyChanges;
            rootElement.Q<Button>(UxmlNames.Revert).clicked += RevertChanges;
        }

        void ApplyChanges()
        {
            var workingDdef = (DeploymentDefinition)m_ChangeTracker.SerializedObject.targetObjects[0];

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


            m_ExcludePathsListView.Rebuild();
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
            public const string ExcludePaths = "Exclude Paths";
            public const string ExcludePathsError = "Exclude Paths Error";
        }
    }
}
