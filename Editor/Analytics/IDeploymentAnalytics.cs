using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Deployment.Editor.Analytics
{
    interface IDeploymentAnalytics
    {
        IDeployEvent BeginDeploy(IReadOnlyDictionary<string, List<IDeploymentItem>> itemsPerProvider, string source);

        interface IDeployEvent
        {
            void SendSuccess();
            void SendFailure(Exception exception);
        }
    }
}
