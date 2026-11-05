namespace Unity.Services.Deployment.Cli
{
    /// <summary>
    /// The constants this package's CLI commands use: their groups, tags, and the other literals they depend on,
    /// so each has one source of truth. Keep one class per package: a const read from another assembly is
    /// compiled into the reader, so a shared class would go stale in every package not rebuilt with it.
    /// </summary>
    static class DeploymentCliConstants
    {
        public const string Group = "services.deployment";
    }
}
