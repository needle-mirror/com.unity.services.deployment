using System;

namespace Unity.Services.Deployment.Editor.Environments
{
    class InvalidEnvironmentException : Exception
    {
        public InvalidEnvironmentException(ValidationResult validationResult)
            : base(validationResult.Error)
        {
        }
    }
}
