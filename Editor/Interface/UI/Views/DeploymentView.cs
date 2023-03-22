using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Unity.Services.Deployment.Editor.Analytics;
using Unity.Services.Deployment.Editor.Commands;
using Unity.Services.Deployment.Editor.DeploymentDefinitions;
using Unity.Services.Deployment.Editor.Interface.UI.Components;
using Unity.Services.Deployment.Editor.Interface.UI.Events;
using Unity.Services.Deployment.Editor.Interface.UI.Serialization;
using Unity.Services.Deployment.Editor.Shared.Infrastructure.Collections;
using Unity.Services.Deployment.Editor.Shared.UI;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Interface.UI.Views
{
    class DeploymentView : ViewBase
    {
        const string k_DeployCommandName = "Deploy";
        const string k_OpenCommandName = "Open";

        readonly CollectionBinding<IDeploymentItemViewModel> m_DeploymentItemBindings;
        readonly CollectionBinding<DeploymentDefinition> m_DeploymentDefinitionBindings;

        IDeploymentViewModel m_DeploymentViewModel;
        IDeploymentWindowAnalytics m_DeploymentWindowAnalytics;
        IDeploymentDefinitionService m_DeploymentDefinitionService;
        ICommandManager m_CommandManager;
        IKeyboardShortcuts m_KeyboardShortcuts;
        ISerializationManager m_SerializationManager;
        TreeViewElement m_TreeViewElement;
        StatusPanel m_StatusPanel;

        public enum ItemRetrieval
        {
            Selected,
            Checked
        }

        protected override string UxmlName => "DeploymentWindow_Deployment";

        public DeploymentView()
        {
            m_DeploymentItemBindings = new CollectionBinding<IDeploymentItemViewModel>(this);
            m_DeploymentDefinitionBindings = new CollectionBinding<DeploymentDefinition>(this);

            m_DeploymentItemBindings.BindCollectionChanged(ObservableCollectionOnCollectionChanged);
            m_DeploymentDefinitionBindings.BindCollectionChanged(ObservableCollectionOnCollectionChanged);
        }

        public void Bind(
            IDeploymentViewModel deploymentViewModel,
            IDeploymentWindowAnalytics deploymentWindowAnalytics,
            IDeploymentDefinitionService deploymentDefinitionService,
            ICommandManager commandManager,
            IKeyboardShortcuts keyboardShortcuts,
            ISerializationManager serializationManager)
        {
            m_DeploymentViewModel = deploymentViewModel;
            m_DeploymentWindowAnalytics = deploymentWindowAnalytics;
            m_DeploymentDefinitionService = deploymentDefinitionService;
            m_CommandManager = commandManager;
            m_KeyboardShortcuts = keyboardShortcuts;
            m_SerializationManager = serializationManager;
            BindGUI();
        }

        void BindGUI()
        {
            RegisterCallback<DeployAllClicked>(_ => DeployAllButtonOnClicked());
            RegisterCallback<DeploySelectedClicked>(_ => DeploySelectedButtonOnClicked());
            RegisterCallback<AttachToPanelEvent>(_ => OnAttachedToPanel());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetachedFromPanel());

            var statusLabel = this.Query<Label>(VisualElementNames.StatusLabel);
            m_StatusPanel = new StatusPanel(statusLabel);

            m_TreeViewElement = this.Query<TreeViewElement>().First();
            m_TreeViewElement.BindGUI(m_KeyboardShortcuts);
            m_TreeViewElement.OnSelectionChanged += OnDeploymentItemSelectionChanged;
            OnDeploymentItemSelectionChanged();

            TryAddDeploymentDefinition(m_DeploymentDefinitionService.DefaultDefinition);

            RegisterCallback<RebuildTreeEvent>(RebuildTreeHandler);

            m_DeploymentItemBindings.Source = m_DeploymentViewModel.DeploymentItems;
            m_DeploymentDefinitionBindings.Source = m_DeploymentDefinitionService.DeploymentDefinitions.AsReadonly();

            m_SerializationManager.Bind(this);
            m_SerializationManager.ApplySerialization();
        }

        void RebuildTreeHandler(RebuildTreeEvent e)
        {
            RebuildTree();
        }

        void OnDeploymentItemSelectionChanged()
        {
            var selectedItems = GetDeploymentViewsForDeployment(ItemRetrieval.Selected)
                .ToList();
            var lastSelected = selectedItems.LastOrDefault();
            if (lastSelected == null)
                m_StatusPanel.Clear();
            else
                m_StatusPanel.SelectedItem = lastSelected.Item;
        }

        async void DeploySelectedButtonOnClicked()
        {
            var checkedDeploymentItems = GetDeploymentViewsForDeployment(ItemRetrieval.Checked)
                .Select(vm => vm.Item)
                .ToList();
            await m_DeploymentViewModel.DeployItemsAsync(checkedDeploymentItems);
        }

        async void DeployAllButtonOnClicked()
        {
            await m_DeploymentViewModel.DeployItemsAsync(m_DeploymentViewModel.DeploymentItems);
        }

        void OnAttachedToPanel()
        {
            m_SerializationManager.Bind(this);
            m_SerializationManager.ApplySerialization();
        }

        void OnDetachedFromPanel()
        {
            m_SerializationManager.Unbind();
        }

        List<DeploymentDefinitionView> GetDeploymentDefinitionViews()
        {
            return this.Query<DeploymentDefinitionView>().ToList();
        }

        IEnumerable<DeploymentItemView> GetAllDeploymentItemViews()
        {
            return GetDeploymentDefinitionViews()
                .SelectMany(definition => definition.GetDeploymentItemViews());
        }

        void BuildContextMenu(DeploymentElementViewBase itemView, ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(k_DeployCommandName, _ => OnItemDeployedFromContextMenuClicked(itemView));

            var postCommandAction = new Dictionary<string, Action>
            {
                { k_OpenCommandName, () => OnContextMenuItemOpened(itemView as DeploymentItemView) }
            };

            var modelSelection = GetSelection().ToList();
            var commands = m_CommandManager.GetCommandsForObjects(modelSelection);
            bool first = true;

            foreach (var command in commands)
            {
                var status = CommandUtils.GetMenuItemStatus(command, modelSelection);

                if (status == DropdownMenuAction.Status.Hidden)
                {
                    continue;
                }

                if (first)
                {
                    evt.menu.AppendSeparator();
                    first = false;
                }

                evt.menu.AppendAction(
                    command.Name,
                    _ =>
                    {
                        command.Execute(modelSelection);
                        if (postCommandAction.TryGetValue(command.Name, out Action action))
                            action();
                    },
                    status);
            }

            evt.StopPropagation();
        }

        internal async void OnItemDeployedFromContextMenuClicked(DeploymentElementViewBase itemView)
        {
            var selectedModels = GetDeploymentViewsForDeployment(ItemRetrieval.Selected)
                .Select(di => di.Item)
                .ToList();
            await m_DeploymentViewModel.DeployItemsAsync(selectedModels);
        }

        IEnumerable<object> GetSelection()
        {
            foreach (var deploymentDefinition in GetDeploymentDefinitionViews())
            {
                if (deploymentDefinition.Selected)
                {
                    yield return deploymentDefinition.DeploymentDefinition;
                }

                foreach (var div in deploymentDefinition.GetSelectedDeploymentItemViews())
                {
                    yield return div.Item.OriginalItem;
                }
            }
        }

        IEnumerable<DeploymentItemView> GetDeploymentViewsForDeployment(ItemRetrieval itemRetrieval)
        {
            return GetDeploymentDefinitionViews().SelectMany(item => item.GetDeploymentViewsForDeployment(itemRetrieval));
        }

        void ObservableCollectionOnCollectionChanged<T>(IReadOnlyCollection<T> collection, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (typeof(T) == typeof(IDeploymentItemViewModel))
                {
                    RemoveOldItems(GetAllDeploymentItemViews().Select(i => i.Item));
                    AddNewItems(collection);
                }
                else if (typeof(T) == typeof(DeploymentDefinition))
                {
                    var toRemove = GetDeploymentDefinitionViews()
                        .Select(i => i.DeploymentDefinition)
                        .Where(m => m != m_DeploymentDefinitionService.DefaultDefinition);
                    RemoveOldItems(toRemove);
                    AddNewItems(collection);
                }
            }
            else
            {
                RemoveOldItems(e.OldItems);
                AddNewItems(e.NewItems);
            }

            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                m_SerializationManager.ApplySerialization();
            }
        }

        void RemoveOldItems(IEnumerable oldItems)
        {
            if (oldItems == null)
                return;

            foreach (var oldItem in oldItems)
            {
                if (oldItem is IDeploymentItemViewModel item)
                    RemoveDeploymentItem(item);
                else if (oldItem is DeploymentDefinition definition)
                {
                    RemoveDeploymentDefinition(definition);
                }
            }
        }

        void AddNewItems(IEnumerable newItems)
        {
            if (newItems == null)
                return;

            foreach (var newItem in newItems)
            {
                if (newItem is IDeploymentItemViewModel item)
                {
                    AddDeploymentDefinitionForItem(item);
                    AddDeploymentItem(item);
                }
                else if (newItem is DeploymentDefinition definition)
                {
                    TryAddDeploymentDefinition(definition);
                }
            }

            RebuildTree();
        }

        DeploymentDefinitionView GetViewFromModel(DeploymentDefinition definition)
        {
            return GetDeploymentDefinitionViews().First(x => x.DeploymentDefinition == definition);
        }

        void AddDeploymentDefinitionForItem(IDeploymentItem deploymentItem)
        {
            var definition = m_DeploymentDefinitionService.DefinitionForPath(deploymentItem.Path)
                ?? m_DeploymentDefinitionService.DefaultDefinition;
            TryAddDeploymentDefinition(definition);
        }

        void TryAddDeploymentDefinition(DeploymentDefinition definition)
        {
            var ddefViews = GetDeploymentDefinitionViews();
            if (ddefViews.All(x => x.DeploymentDefinition != definition))
            {
                AddDeploymentDefinition(definition, ddefViews.Count);
            }
        }

        void AddDeploymentDefinition(DeploymentDefinition definition, int nbViews)
        {
            var definitionView = new DeploymentDefinitionView();
            definitionView.Bind(definition, definition == m_DeploymentDefinitionService.DefaultDefinition);
            definitionView.DoubleClickDeployed += (elementView) => OnDefinitionDeployedFromDoubleClick(elementView as DeploymentDefinitionView);
            definitionView.ContextMenuRequested += BuildContextMenu;
            var index = definition == m_DeploymentDefinitionService.DefaultDefinition
                ? 0
                : m_DeploymentDefinitionService.DeploymentDefinitions.IndexOf(definition) + 1;
            m_TreeViewElement.Insert(Mathf.Clamp(index, 0, nbViews), definitionView);
        }

        void AddDeploymentItem(IDeploymentItemViewModel deploymentItem)
        {
            var itemView = new DeploymentItemView();
            itemView.Bind(deploymentItem);
            itemView.DoubleClickDeployed += (elementView) => OnItemDeployedFromDoubleClick(elementView as DeploymentItemView);
            itemView.ContextMenuRequested += BuildContextMenu;
            AddDeploymentItemView(itemView);
        }

        async void OnDefinitionDeployedFromDoubleClick(DeploymentDefinitionView definitionView)
        {
            var itemModels = definitionView.GetDeploymentItemViews().Select(x => x.Item);
            await m_DeploymentViewModel.DeployItemsAsync(itemModels);
            m_DeploymentWindowAnalytics.SendDoubleClickEvent(definitionView.DeploymentDefinition.Path);
        }

        async void OnItemDeployedFromDoubleClick(DeploymentItemView itemView)
        {
            var itemModel = GetModelFromView(itemView);
            await m_DeploymentViewModel.DeployItemsAsync(new List<IDeploymentItemViewModel> {itemModel});
            m_DeploymentWindowAnalytics.SendDoubleClickEvent(itemModel.Path);
        }

        internal void OnContextMenuItemOpened(DeploymentItemView itemView)
        {
            var model = GetModelFromView(itemView);
            m_DeploymentWindowAnalytics.SendContextMenuOpenEvent(model.Path);
        }

        internal IDeploymentItemViewModel GetModelFromView(DeploymentItemView itemView)
        {
            return GetDeploymentDefinitionViews()
                .SelectMany(v => v.GetDeploymentItemViews())
                .First(x => x == itemView)
                .Item;
        }

        void AddDeploymentItemView(DeploymentItemView itemView)
        {
            var definition = m_DeploymentDefinitionService.DefinitionForPath(itemView.Item.Path)
                ?? m_DeploymentDefinitionService.DefaultDefinition;
            TryAddDeploymentDefinition(definition);

            GetViewFromModel(definition).AddChild(itemView);
        }

        void RebuildTree()
        {
            var deploymentItemViews = new List<DeploymentItemView>();

            foreach (var definitionView in GetDeploymentDefinitionViews())
            {
                foreach (var itemView in definitionView.GetDeploymentItemViews())
                {
                    deploymentItemViews.Add(itemView);
                    definitionView.RemoveChild(itemView);
                }
            }

            deploymentItemViews.ForEach(AddDeploymentItemView);
        }

        void RemoveDeploymentItem(IDeploymentItemViewModel item)
        {
            foreach (var deploymentDefinitionView in GetDeploymentDefinitionViews())
            {
                var view = deploymentDefinitionView.GetDeploymentItemViews().SingleOrDefault(v => v.Item == item);
                if (view != null)
                {
                    view.Unbind();
                    deploymentDefinitionView.RemoveChild(view);
                    if (view.Selected)
                    {
                        m_StatusPanel.Clear();
                    }
                }
            }
        }

        void RemoveDeploymentDefinition(DeploymentDefinition definition)
        {
            var definitionViewModel = GetViewFromModel(definition);
            var deploymentItems = definitionViewModel.GetDeploymentItemViews();
            m_TreeViewElement.Remove(definitionViewModel);
            deploymentItems.ForEach(AddDeploymentItemView);
        }

        internal static class VisualElementNames
        {
            public const string StatusLabel = "StatusLabel";
        }

        new class UxmlFactory : UxmlFactory<DeploymentView> {}
    }
}
