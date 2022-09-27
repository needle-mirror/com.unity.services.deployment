using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Environments;
using Unity.Services.Deployment.Editor.Shared.Collections;
using Unity.Services.DeploymentApi.Editor;
using Logger = Unity.Services.Deployment.Editor.Shared.Logging.Logger;

namespace Unity.Services.Deployment.Editor.Interface
{
    sealed class DeploymentViewModel : IDeploymentViewModel, IDisposable
    {
        public IReadOnlyObservable<IDeploymentItemViewModel> DeploymentItems => m_MergedViewModels;

        readonly IEnvironmentValidator m_EnvironmentValidator;
        readonly ObservableCollection<DeploymentProvider> m_DeploymentProviders;
        readonly MergedObservableCollection<DeploymentItemViewModel> m_MergedViewModels;

        public DeploymentViewModel(
            IEnvironmentValidator environmentValidator,
            ObservableCollection<DeploymentProvider> deploymentProviders)
        {
            m_EnvironmentValidator = environmentValidator;
            m_DeploymentProviders = deploymentProviders;

            m_MergedViewModels = new MergedObservableCollection<DeploymentItemViewModel>(deploymentProviders
                .Select(MapToViewModels));

            m_DeploymentProviders.CollectionChanged += DeploymentProvidersOnCollectionChanged;
        }

        public async Task DeployItemsAsync(IEnumerable<IDeploymentItemViewModel> items)
        {
            var validationResult = await m_EnvironmentValidator.ValidateEnvironmentAsync();

            if (validationResult.Failed)
            {
                Logger.LogError(validationResult.Error);
                items.ForEach(item => item.Status = new DeploymentStatus(
                    "Invalid Environment",
                    validationResult.Error));
                return;
            }

            var providerCommands = m_DeploymentProviders.Select(provider => new Tuple<DeploymentProvider, Command>(provider, provider.DeployCommand)).ToList();
            await ExecuteCommandAsync(providerCommands, items.Cast<DeploymentItemViewModel>());
        }

        void DeploymentProvidersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    e.NewItems.Cast<DeploymentProvider>()
                        .Select(MapToViewModels)
                        .ForEach(m_MergedViewModels.AddCollection);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count > 1)
                    {
                        throw new NotImplementedException("Can not remove more than 1 DeploymentProvider at a time");
                    }
                    m_MergedViewModels.RemoveCollectionAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    m_MergedViewModels.ClearCollections();
                    m_DeploymentProviders
                        .Select(MapToViewModels)
                        .ForEach(m_MergedViewModels.AddCollection);
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException($"{nameof(DeploymentViewModel)} does not support {e.Action}");
                default:
                    throw new ArgumentOutOfRangeException($"Unknown action {e.Action}");
            }
        }

        static IReadOnlyObservable<DeploymentItemViewModel> MapToViewModels(DeploymentProvider provider)
        {
            return provider.DeploymentItems.Map(i => new DeploymentItemViewModel(i, provider.Service));
        }

        static async Task ExecuteCommandAsync(IEnumerable<Tuple<DeploymentProvider, Command>> providerCommands, IEnumerable<DeploymentItemViewModel> itemViewModels)
        {
            var deploymentItemViewModels = itemViewModels.EnumerateOnce();

            var commandTasks = new List<Task>();

            foreach (var tuple in providerCommands)
            {
                var provider = tuple.Item1;
                var command = tuple.Item2;

                var providerItems = provider.DeploymentItems
                    .Intersect(deploymentItemViewModels.Select(i => i.OriginalItem))
                    .EnumerateOnce();

                if (!providerItems.Any())
                {
                    continue;
                }

                commandTasks.Add(command.ExecuteAsync(providerItems));
            }

            await Task.WhenAll(commandTasks);
        }

        public void Dispose()
        {
            m_DeploymentProviders.CollectionChanged -= DeploymentProvidersOnCollectionChanged;
            m_MergedViewModels.Dispose();
        }
    }
}
