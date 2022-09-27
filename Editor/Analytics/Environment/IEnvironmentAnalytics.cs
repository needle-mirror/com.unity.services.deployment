namespace Unity.Services.Deployment.Editor.Analytics.Environment
{
    interface IEnvironmentAnalytics
    {
        void RegisterEnvironmentChangedEvent();
        void SendEnvironmentChangedEvent(EnvironmentChangedParameters environmentChangedParameters);
    }
}
