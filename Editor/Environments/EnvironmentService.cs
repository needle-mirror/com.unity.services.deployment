using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.Services.Deployment.Editor.Analytics.Environment;
using Unity.Services.Deployment.Editor.Configuration;
using Unity.Services.Deployment.Editor.Shared.EditorUtils;
using UnityEditor;

namespace Unity.Services.Deployment.Editor.Environments
{
    sealed class EnvironmentService : IEnvironmentService, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IReadOnlyCollection<EnvironmentInfo> Environments { get; private set; }
        public Guid? ActiveEnvironmentId
        {
            get
            {
                if (Guid.TryParse(m_DeploymentSettings.EnvironmentGuid, out var envId))
                {
                    return envId;
                }
                return null;
            }
        }

        readonly IEnvironmentFetcher m_EnvironmentFetcher;
        readonly IDeploymentSettings m_DeploymentSettings;
        readonly IEnvironmentAnalytics m_EnvironmentAnalytics;
        readonly EditorValueTracker<string> m_ProjectIdTracker;

        Task m_RefreshTask;

        public EnvironmentService(IEnvironmentFetcher environmentFetcher, IDeploymentSettings deploymentSettings, IEnvironmentAnalytics environmentAnalytics)
        {
            m_EnvironmentFetcher = environmentFetcher;
            m_DeploymentSettings = deploymentSettings;
            m_EnvironmentAnalytics = environmentAnalytics;

            environmentAnalytics.RegisterEnvironmentChangedEvent();

            m_ProjectIdTracker = new EditorValueTracker<string>(() => CloudProjectSettings.projectId);
            m_ProjectIdTracker.ValueChanged += (_, _) =>
            {
                if (string.IsNullOrEmpty(CloudProjectSettings.projectId))
                {
                    return;
                }

                RefreshAsync();
            };
        }

        public Task RefreshAsync()
        {
            if (m_RefreshTask == null || m_RefreshTask.IsCompleted)
            {
                m_RefreshTask = RefreshInternal();
            }

            return m_RefreshTask;
        }

        public void SetActiveEnvironment(EnvironmentInfo environment)
        {
            m_DeploymentSettings.EnvironmentGuid = environment.Id.ToString();
            m_EnvironmentAnalytics.SendEnvironmentChangedEvent(new EnvironmentChangedParameters(ActiveEnvironmentId.ToString()));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveEnvironmentId)));
        }

        async Task RefreshInternal()
        {
            Environments = null;
            Environments = await m_EnvironmentFetcher.FetchEnvironments();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Environments)));
        }

        public void Dispose()
        {
            m_ProjectIdTracker.Dispose();
            m_RefreshTask?.Dispose();
        }
    }
}
