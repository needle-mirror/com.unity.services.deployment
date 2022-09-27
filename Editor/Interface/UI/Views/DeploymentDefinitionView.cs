using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Deployment.Editor.DeploymentDefinitions;
using Unity.Services.Deployment.Editor.Interface.UI.Events;
using Unity.Services.Deployment.Editor.Shared.UI;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Interface.UI.Views
{
    class DeploymentDefinitionView : DeploymentElementViewBase
    {
        const string k_TemplatePath = "Packages/com.unity.services.deployment/Editor/Interface/UI/Assets/Templates/DeploymentDefinitionTemplate.uxml";

        public DeploymentDefinition DeploymentDefinition { get; private set; }

        readonly ModelBinding<DeploymentDefinition> m_ItemBindings;
        bool m_IsDefault;
        string m_Path;

        public DeploymentDefinitionView()
            : base(k_TemplatePath)
        {
            m_ItemBindings = new ModelBinding<DeploymentDefinition>(this);
            m_ItemBindings.BindProperty(nameof(DeploymentDefinitions.DeploymentDefinition.Name), def =>
            {
                this.Q<Label>(VisualElementNames.DefinitionName).text = def.Name;
            });
            m_ItemBindings.BindProperty(nameof(DeploymentDefinitions.DeploymentDefinition.Path), def =>
            {
                if (m_Path != def.Path)
                {
                    m_Path = def.Path;
                    RebuildTreeEvent.Send(this);
                }
            });
        }

        public void Bind(DeploymentDefinition definition, bool isDefault)
        {
            m_IsDefault = isDefault;
            DeploymentDefinition = definition;
            base.Model = definition;
            m_ItemBindings.Source = definition;

            RefreshVisibility();
        }

        public void AddChild(DeploymentItemView itemView)
        {
            this.Q(VisualElementNames.ContainerElement).Add(itemView);
            this.Q(VisualElementNames.ContainerElement).Sort((a, b) =>
            {
                var itemA = (DeploymentItemView)a;
                var itemB = (DeploymentItemView)b;
                var itemAName = itemA.Item.Name ?? itemA.Item.Service;
                var itemBName = itemB.Item.Name ?? itemB.Item.Service;
                return string.Compare(itemAName, itemBName, StringComparison.Ordinal);
            });
            RefreshVisibility();
        }

        public void RemoveChild(DeploymentItemView itemView)
        {
            this.Q(VisualElementNames.ContainerElement).Remove(itemView);
            RefreshVisibility();
        }

        public List<DeploymentItemView> GetDeploymentItemViews()
        {
            return this.Query<DeploymentItemView>().ToList();
        }

        public IEnumerable<DeploymentItemView> GetDeploymentViewsForDeployment()
        {
            if (Selected)
            {
                return GetDeploymentItemViews();
            }

            return GetDeploymentItemViews().Where(itemView => itemView.Selected);
        }

        public IEnumerable<DeploymentItemView> GetSelectedDeploymentItemViews()
        {
            return GetDeploymentItemViews()
                .Where(i => i.Selected);
        }

        void RefreshVisibility()
        {
            if (m_IsDefault)
            {
                var hasItems = GetDeploymentItemViews().Any();
                visible = hasItems;
                style.display = hasItems
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        internal static class VisualElementNames
        {
            public const string DefinitionName = "DeploymentSetName";
            public const string ContainerElement = "DeploymentSetContainer";
        }

        new class UxmlFactory : UxmlFactory<DeploymentDefinitionView> {}
    }
}
