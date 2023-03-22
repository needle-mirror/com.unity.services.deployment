using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Unity.Services.Deployment.Editor.Environments
{
    interface IEnvironmentService : INotifyPropertyChanged
    {
        IReadOnlyCollection<EnvironmentInfo> Environments { get; }
        Guid? ActiveEnvironmentId { get; }
        Task RefreshAsync();
        void SetActiveEnvironment(EnvironmentInfo environment);
    }
}
