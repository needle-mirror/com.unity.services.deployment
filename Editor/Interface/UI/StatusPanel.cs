using System.ComponentModel;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine.UIElements;

namespace Unity.Services.Deployment.Editor.Interface.UI
{
    class StatusPanel
    {
        const string k_Status = "Status";

        readonly Label m_StatusLabel;
        IDeploymentItem m_SelectedItem;

        public IDeploymentItem SelectedItem
        {
            get => m_SelectedItem;
            set
            {
                if (m_SelectedItem != null)
                {
                    m_SelectedItem.PropertyChanged -= OnItemPropertyChanged;
                }

                m_SelectedItem = value;
                m_SelectedItem.PropertyChanged += OnItemPropertyChanged;

                UpdateStatus();
            }
        }

        public StatusPanel(Label statusLabel)
        {
            m_StatusLabel = statusLabel;
        }

        public void Clear()
        {
            m_StatusLabel.text = string.Empty;
        }

        void UpdateStatus()
        {
            var itemStatus = m_SelectedItem.Status;
            var status = $"{itemStatus.Message}\n{itemStatus.MessageDetail}";

            m_StatusLabel.text = status;
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == k_Status)
            {
                UpdateStatus();
            }
        }
    }
}
