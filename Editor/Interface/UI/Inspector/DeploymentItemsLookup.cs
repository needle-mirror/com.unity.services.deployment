using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Interface.UI.Inspector
{
    /// <summary>
    /// Lookup table for <see cref="IDeploymentItem"/>s based on their path.<br />
    /// The class registers to the <see cref="Deployments"/> collection changes to update the lookup table
    /// with the list of all <see cref="IDeploymentItem"/>s sorted by their asset path.<br />
    /// </summary>
    class DeploymentItemsLookup
    {
        /// <summary>
        /// Lookup of <see cref="DeploymentItem"/> instance based on their path.<br />
        /// This is updated whenever the list of deployment items or providers changes.
        /// </summary>
        readonly Dictionary<string, List<IDeploymentItem>> m_DeploymentLookup = new();

        /// <summary>
        /// Items currently subscribed to <see cref="IDeploymentItem.PropertyChanged"/>
        /// so we can unsubscribe before rebuilding the lookup.
        /// </summary>
        readonly List<IDeploymentItem> m_SubscribedItems = new();

        /// <summary>
        /// The deployments instance that contains the list of deployment providers and items.
        /// </summary>
        readonly Deployments m_Deployments;

        public DeploymentItemsLookup(Deployments deployments)
        {
            m_Deployments = deployments;
            m_Deployments.DeploymentProviders.CollectionChanged += OnDeploymentListChanged;
            RefreshLookup();
        }

        /// <summary>
        /// Output the list of <see cref="IDeploymentItem"/>s at the specified path.<br />
        /// If the path is not valid or no items are found, it will return false and a null list.
        /// </summary>
        /// <param name="path">An asset path.</param>
        /// <param name="items">List of <see cref="IDeploymentItem"/>s registered at that path.</param>
        /// <returns>True is at least one <see cref="IDeploymentItem"/> exists at the given path, false otherwise.</returns>
        public bool TryGetDeploymentItemsAtPath(string path, out List<IDeploymentItem> items)
        {
            if (string.IsNullOrEmpty(path))
            {
                items = null;
                return false;
            }

            return m_DeploymentLookup.TryGetValue(path, out items);
        }

        /// <summary>
        /// Update the lookup table with the current list of <see cref="IDeploymentItem"/>s.<br />
        /// This method will also register to the <see cref="DeploymentProvider.DeploymentItems.CollectionChanged"/> event on each provider
        /// and to each item's <see cref="IDeploymentItem.PropertyChanged"/> event to detect path changes.
        /// </summary>
        void RefreshLookup()
        {
            foreach (var item in m_SubscribedItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
            m_SubscribedItems.Clear();

            m_DeploymentLookup.Clear();
            foreach (var provider in m_Deployments.DeploymentProviders)
            {
                provider.DeploymentItems.CollectionChanged -= OnDeploymentListChanged;
                provider.DeploymentItems.CollectionChanged += OnDeploymentListChanged;
                foreach (var deploymentItem in provider.DeploymentItems)
                {
                    deploymentItem.PropertyChanged -= OnItemPropertyChanged;
                    deploymentItem.PropertyChanged += OnItemPropertyChanged;
                    m_SubscribedItems.Add(deploymentItem);

                    if (string.IsNullOrEmpty(deploymentItem.Path))
                        continue;

                    if (!m_DeploymentLookup.ContainsKey(deploymentItem.Path))
                    {
                        m_DeploymentLookup.Add(deploymentItem.Path, new List<IDeploymentItem>());
                    }
                    m_DeploymentLookup[deploymentItem.Path].Add(deploymentItem);
                }
            }
        }

        void OnDeploymentListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshLookup();
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDeploymentItem.Path))
            {
                RefreshLookup();
            }
        }
    }
}
