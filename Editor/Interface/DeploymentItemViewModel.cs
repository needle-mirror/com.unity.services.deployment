using System.Collections.ObjectModel;
using System.ComponentModel;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Interface
{
    class DeploymentItemViewModel : IDeploymentItemViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => OriginalItem.PropertyChanged += value;
            remove => OriginalItem.PropertyChanged -= value;
        }
        public string Name => OriginalItem.Name;
        public string Path => OriginalItem.Path;
        public float Progress => OriginalItem.Progress;
        public DeploymentStatus Status
        {
            get => OriginalItem.Status;
            set => OriginalItem.Status = value;
        }

        public ObservableCollection<AssetState> States => OriginalItem.States;
        public string Service { get; }
        public IDeploymentItem OriginalItem { get; }

        public DeploymentItemViewModel(IDeploymentItem originalItem, string service)
        {
            Service = service;
            OriginalItem = originalItem;
        }
    }
}
