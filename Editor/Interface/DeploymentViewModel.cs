using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Analytics;
using Unity.Services.Deployment.Editor.Environments;
using Unity.Services.Deployment.Editor.Shared.Infrastructure.Collections;
using Unity.Services.Deployment.Editor.Shared.Logging;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Interface
{
    sealed class DeploymentViewModel : IDeploymentViewModel, IDisposable
    {
        const string k_AnalyticsSource = "deployment window";

        public IReadOnlyObservable<IDeploymentItemViewModel> DeploymentItems => m_MergedViewModels;

        readonly IEnvironmentValidator m_EnvironmentValidator;
        readonly ObservableCollection<DeploymentProvider> m_DeploymentProviders;
        readonly MergedObservableCollection<DeploymentItemViewModel> m_MergedViewModels;
        readonly IDeploymentAnalytics m_Analytics;

        public DeploymentViewModel(
            IEnvironmentValidator environmentValidator,
            ObservableCollection<DeploymentProvider> deploymentProviders,
            IDeploymentAnalytics analytics)
        {
            m_EnvironmentValidator = environmentValidator;
            m_DeploymentProviders = deploymentProviders;
            m_Analytics = analytics;

            m_MergedViewModels = new MergedObservableCollection<DeploymentItemViewModel>(deploymentProviders
                .Select(MapToViewModels));

            m_DeploymentProviders.CollectionChanged += DeploymentProvidersOnCollectionChanged;
        }

        public async Task DeployItemsAsync(IEnumerable<IDeploymentItemViewModel> items)
        {
            var enumeratedItems = items.EnumerateOnce();
            enumeratedItems.ForEach(item => item.IsBeingDeployed = true);
            var analytics = m_Analytics.BeginDeploy(ItemsPerProvider(enumeratedItems), k_AnalyticsSource);
            try
            {
                await ValidateEnvironment(enumeratedItems);
                await ExecuteCommandAsync(enumeratedItems, p => p.DeployCommand);
                analytics.SendSuccess();
            }
            catch (Exception e)
            {
                analytics.SendFailure(e);
                Logger.LogException(e);
                throw;
            }
            finally
            {
                enumeratedItems.ForEach(item => item.IsBeingDeployed = false);
            }
        }

        async Task ValidateEnvironment(IEnumerable<IDeploymentItemViewModel> items)
        {
            var validationResult = await m_EnvironmentValidator.ValidateEnvironmentAsync();
            if (validationResult.Failed)
            {
                items.ForEach(item => item.Status = new DeploymentStatus(
                    "Invalid Environment",
                    validationResult.Error));
                throw new InvalidEnvironmentException(validationResult);
            }
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

        Task ExecuteCommandAsync(IReadOnlyCollection<IDeploymentItemViewModel> deploymentItemViewModels, Func<DeploymentProvider, Command> command)
        {
            var providerCommands = m_DeploymentProviders
                .Select(provider => new Tuple<DeploymentProvider, Command>(provider, command(provider)))
                .Where(tuple => tuple.Item2 != null)
                .ToList();
            return ExecuteCommandAsync(providerCommands, deploymentItemViewModels);
        }

        static IReadOnlyDictionary<string, List<IDeploymentItem>> ItemsPerProvider(IReadOnlyCollection<IDeploymentItemViewModel> items)
        {
            var mapping = new Dictionary<string, List<IDeploymentItem>>();
            foreach (var item in items)
            {
                var key = item.Service ?? "NULL";
                if (!mapping.ContainsKey(key))
                {
                    mapping[key] = new List<IDeploymentItem>();
                }
                mapping[key].Add(item.OriginalItem);
            }

            return mapping;
        }

        static async Task ExecuteCommandAsync(IEnumerable<Tuple<DeploymentProvider, Command>> providerCommands, IReadOnlyCollection<IDeploymentItemViewModel> deploymentItemViewModels)
        {
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
