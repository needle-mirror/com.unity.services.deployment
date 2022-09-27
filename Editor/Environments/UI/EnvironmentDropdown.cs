using System.Collections.Generic;
using System.Linq;
using Unity.Services.Deployment.Editor.Analytics.Environment;
using Unity.Services.Deployment.Editor.Configuration;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Environments.UI
{
    class EnvironmentDropdown
    {
        readonly IEnvironmentAnalytics m_EnvironmentAnalytics;
        readonly IDeploymentSettings m_EnvironmentProvider;
        Dictionary<string, EnvironmentInfo> m_EnvironmentMap;

        public bool IsReady { get; private set; }

        public EnvironmentDropdown(IEnvironmentAnalytics environmentAnalytics, IDeploymentSettings environmentProvider)
        {
            m_EnvironmentAnalytics = environmentAnalytics;
            m_EnvironmentProvider = environmentProvider;
            m_EnvironmentMap = new Dictionary<string, EnvironmentInfo>();
        }

        public void Setup(VisualElement containerElement, Dictionary<string, EnvironmentInfo> environmentMap)
        {
            m_EnvironmentMap = environmentMap;

            var dropdownField = containerElement.Q<DropdownField>();
            dropdownField.choices = m_EnvironmentMap.Values.Select(env => env.Name).ToList();
            dropdownField.RegisterValueChangedCallback(OnDropdownValueChanged);

            var currentEnvInfo = m_EnvironmentMap.Values.FirstOrDefault(x => x.Id == m_EnvironmentProvider.EnvironmentGuid);

            if (string.IsNullOrEmpty(currentEnvInfo.Id)
                && m_EnvironmentProvider.EnvironmentGuid != currentEnvInfo.Id)
            {
                m_EnvironmentProvider.EnvironmentGuid = string.Empty;
            }

            if (!string.IsNullOrEmpty(currentEnvInfo.Name) && dropdownField.choices.Contains(currentEnvInfo.Name))
                dropdownField.SetValueWithoutNotify(currentEnvInfo.Name);

            IsReady = true;
        }

        public void Reset()
        {
            m_EnvironmentMap.Clear();
            IsReady = false;
        }

        void OnDropdownValueChanged(ChangeEvent<string> changeEvent)
        {
            m_EnvironmentProvider.EnvironmentGuid = m_EnvironmentMap[changeEvent.newValue].Id;
            m_EnvironmentAnalytics.SendEnvironmentChangedEvent(new EnvironmentChangedParameters(changeEvent.newValue));
        }
    }
}
