using System.Threading.Tasks;

namespace Unity.Services.Deployment.Editor.Environments
{
    class ValidationResult
    {
        public bool Failed => !string.IsNullOrEmpty(Error);
        public string Error { get; set; }

        public ValidationResult()
        {
        }
    }

    interface IEnvironmentValidator
    {
        Task<ValidationResult> ValidateEnvironmentAsync();
    }
}
