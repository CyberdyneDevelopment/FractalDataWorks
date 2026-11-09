using Microsoft.Extensions.DependencyInjection;

namespace Fdw.ServiceTypes;

/// <summary>
/// Marks a host whose service provider was set to validate lifetimes by <see cref="PlatformServices.Configure"/> (FDW-4640), and holds the options it was set to.
/// </summary>
/// <remarks>
/// Why a marker: the options of a built provider cannot be read back, so <see cref="PlatformServices.Initialize"/> asks the built host for this registration to learn whether
/// Configure ran on its builder. A host that skipped Configure has no marker and does not start.
/// </remarks>
public sealed class ContainerValidation
{
    /// <summary>The options every host's service provider is built with: a scoped service is refused from the root provider, and every constructor is checked when the host builds.</summary>
    public static ServiceProviderOptions Options { get; } = new() { ValidateScopes = true, ValidateOnBuild = true };
}
