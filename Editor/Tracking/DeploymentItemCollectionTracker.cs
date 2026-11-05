using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Unity.Services.Deployment.Editor.Shared.Infrastructure.Collections;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Tracking
{
    /// <summary>
    /// Observes an <see cref="ObservableCollection{DeploymentProvider}"/> and provides a unified view
    /// of all <see cref="IDeploymentItem"/>s across all providers, raising events when items are
    /// added, removed, reset from those providers or when their properties change.
    /// </summary>
    sealed class DeploymentItemCollectionTracker : IDeploymentItemCollectionTracker, IDisposable
    {
        readonly ObservableCollection<DeploymentProvider> m_Providers;
        MergedObservableCollection<IDeploymentItem> m_MergedItems;

        public IEnumerable<IDeploymentItem> Items => m_MergedItems;

        public event Action<IDeploymentItem> ItemAdded;
        public event Action<IDeploymentItem> ItemRemoved;
        public event Action ItemsReset;
        public event Action<IDeploymentItem, string> ItemPropertyChanged;

        public DeploymentItemCollectionTracker(ObservableCollection<DeploymentProvider> providers)
        {
            m_Providers = providers;
            m_MergedItems = new MergedObservableCollection<IDeploymentItem>(
                m_Providers.Select(p => p.DeploymentItems.AsReadonly()));

            m_Providers.CollectionChanged += OnProvidersCollectionChanged;
            m_MergedItems.CollectionChanged += OnItemsCollectionChanged;
            m_MergedItems.ForEach(RegisterItem);
        }

        public void Dispose()
        {
            m_Providers.CollectionChanged -= OnProvidersCollectionChanged;
            m_MergedItems.CollectionChanged -= OnItemsCollectionChanged;
            m_MergedItems.ForEach(UnregisterItem);
        }

        void OnProvidersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var provider in e.NewItems.Cast<DeploymentProvider>())
                        m_MergedItems.AddCollection(provider.DeploymentItems.AsReadonly());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count > 1)
                    {
                        throw new NotImplementedException("Can not remove more than 1 DeploymentProvider at a time");
                    }
                    foreach (var _ in e.OldItems.Cast<DeploymentProvider>())
                        m_MergedItems.RemoveCollectionAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    m_MergedItems.CollectionChanged -= OnItemsCollectionChanged;
                    m_MergedItems.ForEach(UnregisterItem);
                    m_MergedItems.ClearCollections();
                    m_MergedItems = new MergedObservableCollection<IDeploymentItem>(
                        m_Providers.Select(p => p.DeploymentItems.AsReadonly()));
                    m_MergedItems.CollectionChanged += OnItemsCollectionChanged;
                    m_MergedItems.ForEach(RegisterItem);
                    ItemsReset?.Invoke();
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException($"{nameof(DeploymentItemCollectionTracker)} does not support {e.Action}");
                default:
                    throw new ArgumentOutOfRangeException($"Unknown action {e.Action}");
            }
        }

        void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.Cast<IDeploymentItem>())
                    {
                        UnregisterItem(item);
                        RegisterItem(item);
                        ItemAdded?.Invoke(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.Cast<IDeploymentItem>())
                    {
                        UnregisterItem(item);
                        ItemRemoved?.Invoke(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    m_MergedItems.ForEach(UnregisterItem);
                    m_MergedItems.ForEach(RegisterItem);
                    ItemsReset?.Invoke();
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException($"{nameof(DeploymentItemCollectionTracker)} does not support {e.Action}");
                default:
                    throw new ArgumentOutOfRangeException($"Unknown action {e.Action}");
            }
        }

        void RegisterItem(IDeploymentItem item)
        {
            if (item == null)
                return;
            item.PropertyChanged += OnItemPropertyChanged;
        }

        void UnregisterItem(IDeploymentItem item)
        {
            if (item == null)
                return;
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ItemPropertyChanged?.Invoke((IDeploymentItem)sender, e.PropertyName);
        }
    }
}
