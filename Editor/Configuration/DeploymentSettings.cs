using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Services.Deployment.Editor.Shared.EditorUtils;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;

namespace Unity.Services.Deployment.Editor.Configuration
{
    class DeploymentSettings : IDeploymentSettings, IEnvironmentProvider, IDisposable
    {
        const string k_DeployOnPlayToggleKey = "Deployment_DeployOnPlay";
        const string k_BlockPlaymodeOnFailure = "Deployment_BlockPlaymodeOnFailure";
        const string k_EnvironmentGuid = "Deployment_EnvironmentGuid";
        EditorValueTracker<string> m_ProjectIdTracker;
        IProjectPreferences m_ProjectPreferences;

        public DeploymentSettings(IProjectPreferences projectProjectPreferences)
        {
            var projectHasBeenSetPreviously = !string.IsNullOrEmpty(CloudProjectSettings.projectId);
            m_ProjectPreferences = projectProjectPreferences;
            m_ProjectIdTracker = new EditorValueTracker<string>(() => CloudProjectSettings.projectId);
            m_ProjectIdTracker.ValueChanged += (_, _) =>
            {
                if (projectHasBeenSetPreviously)
                {
                    EnvironmentGuid = string.Empty;
                }

                if (!string.IsNullOrEmpty(CloudProjectSettings.projectId))
                {
                    projectHasBeenSetPreviously = true;
                }
            };
        }

        public bool ShouldDeployOnPlay
        {
            get => m_ProjectPreferences.GetBool(k_DeployOnPlayToggleKey);
            set
            {
                m_ProjectPreferences.SetBool(k_DeployOnPlayToggleKey, value);
                OnPropertyChanged();
            }
        }

        public bool BlockPlaymodeOnFailure
        {
            get => m_ProjectPreferences.GetBool(k_BlockPlaymodeOnFailure);
            set
            {
                m_ProjectPreferences.SetBool(k_BlockPlaymodeOnFailure, value);
                OnPropertyChanged();
            }
        }

        public string EnvironmentGuid
        {
            get => m_ProjectPreferences.GetString(k_EnvironmentGuid);
            set
            {
                m_ProjectPreferences.SetString(k_EnvironmentGuid, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Current));
            }
        }

        public string Current => EnvironmentGuid;

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        public void Dispose()
        {
            m_ProjectIdTracker.Dispose();
        }
    }
}
