using Fdw.Services.Abstractions;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>
/// Supplies logging configuration. Registered and resolved as this type — never as the base.
/// </summary>
/// <remarks>
/// It reads through the <c>ServerConfiguration</c> connection rather than
/// <c>PlatformConfiguration</c>: logging has to come up before the platform store is reachable, so
/// its configuration lives in the file-backed server tier declared in <c>configurationSchema.json</c>.
/// </remarks>
public interface ILoggingConfigurationProvider
    : IDomainConfigurationProvider<ILoggingImplementationConfiguration>
{
}
