using System;

namespace Unity.Services.Deployment.Editor.Analytics.Environment
{
    [Serializable]
    struct EnvironmentChangedParameters
    {
        public string environmentGuid;

        public EnvironmentChangedParameters(string environmentGuid)
        {
            this.environmentGuid = environmentGuid;
        }
    }
}
