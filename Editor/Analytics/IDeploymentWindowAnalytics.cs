namespace Unity.Services.Deployment.Editor.Analytics
{
    interface IDeploymentWindowAnalytics
    {
        public void SendDoubleClickEvent(string itemPath);
        public void SendContextMenuOpenEvent(string itemPath);
        public void SendContextMenuSelectEvent(string itemPath);
    }
}
