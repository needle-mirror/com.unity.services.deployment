using Unity.Services.Deployment.Editor.Interface.UI.Events;
using Unity.Services.Deployment.Editor.Shared.UI;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine.UIElements;
using ProgressBar = UnityEngine.UIElements.ProgressBar;

namespace Unity.Services.Deployment.Editor.Interface.UI.Views
{
    class DeploymentItemView : DeploymentElementViewBase
    {
        const string k_TemplatePath = "Packages/com.unity.services.deployment/Editor/Interface/UI/Assets/Templates/DeploymentItemTemplate.uxml";
        const string k_WarningClassName = "warning";
        const string k_ErrorClassName = "error";

        public IDeploymentItemViewModel Item { get; private set; }

        readonly ModelBinding<IDeploymentItemViewModel> m_ItemBindings;
        string m_Path;

        public DeploymentItemView()
            : base(k_TemplatePath)
        {
            m_ItemBindings = new ModelBinding<IDeploymentItemViewModel>(this);

            m_ItemBindings.BindProperty(nameof(Item.Name), item =>
            {
                var itemNameLabel = this.Q<Label>(VisualElementNames.ItemName);
                itemNameLabel.text = item.Name;
            });
            m_ItemBindings.BindProperty(nameof(Item.Progress), item =>
            {
                var progressBar = this.Q<ProgressBar>();
                progressBar.value = item.Progress;
                progressBar.title = $"{item.Progress:0.##}%";
            });
            m_ItemBindings.BindProperty(nameof(Item.Status), item =>
            {
                SetStatus(item.Status);
            });
            m_ItemBindings.BindProperty(nameof(Item.Path), item =>
            {
                if (m_Path != item.Path)
                {
                    m_Path = item.Path;
                    RebuildTreeEvent.Send(this);
                }
            });
        }

        public void Bind(IDeploymentItemViewModel item)
        {
            Item = item;
            Model = item;
            m_ItemBindings.Source = item;

            this.Q<Label>(VisualElementNames.ItemService).text = item.Service;
        }

        protected override void OnClick(ClickEvent click)
        {
            base.OnClick(click);

            if (click.clickCount == 2)
            {
                click.StopPropagation();
            }
        }

        void SetStatus(DeploymentStatus status)
        {
            SetStatusClass(status);
            var itemStatusLabel = this.Q<Label>(VisualElementNames.ItemStatus);
            itemStatusLabel.text = status.Message;
        }

        void SetStatusClass(DeploymentStatus status)
        {
            RemoveFromClassList(k_ErrorClassName);
            RemoveFromClassList(k_WarningClassName);

            switch (status.MessageSeverity)
            {
                case SeverityLevel.Error:
                    AddToClassList(k_ErrorClassName);
                    break;
                case SeverityLevel.Warning:
                    AddToClassList(k_WarningClassName);
                    break;
            }
        }

        new class UxmlFactory : UxmlFactory<DeploymentItemView> {} //NOSONAR

        internal static class VisualElementNames
        {
            public const string ItemName = "ItemName";
            public const string ItemStatus = "ItemStatus";
            public const string ItemStatusIcon = "ItemStatusIcon";
            public const string ItemService = "ItemService";
        }
    }
}
