using System;
using System.Linq;

namespace Unity.Services.Deployment.Editor.Environments
{
    static class EnvironmentServiceExtensions
    {
        public static EnvironmentInfo? ActiveEnvironmentInfo(this IEnvironmentService service)
        {
            return service.EnvironmentInfoFromId(service.ActiveEnvironmentId);
        }

        public static EnvironmentInfo? EnvironmentInfoFromId(this IEnvironmentService service, Guid? environmentId)
        {
            if (environmentId == null)
            {
                return null;
            }

            return service.Environments ?
                .Where(info => info.Id == environmentId)
                .Cast<EnvironmentInfo?>() // ensure FirstOrDefault will return null instead of default(EnvironmentInfo)
                .FirstOrDefault();
        }

        public static EnvironmentInfo? EnvironmentInfoFromName(this IEnvironmentService service, string environmentName)
        {
            return service.Environments ?
                .Where(info => info.Name == environmentName)
                .Cast<EnvironmentInfo?>() // ensure FirstOrDefault will return null instead of default(EnvironmentInfo)
                .FirstOrDefault();
        }
    }
}
