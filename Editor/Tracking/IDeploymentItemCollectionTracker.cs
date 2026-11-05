using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Tracking
{
    interface IDeploymentItemCollectionTracker
    {
        IEnumerable<IDeploymentItem> Items { get; }
        event Action<IDeploymentItem> ItemAdded;
        event Action<IDeploymentItem> ItemRemoved;
        event Action ItemsReset;
        event Action<IDeploymentItem, string> ItemPropertyChanged;
    }
}
