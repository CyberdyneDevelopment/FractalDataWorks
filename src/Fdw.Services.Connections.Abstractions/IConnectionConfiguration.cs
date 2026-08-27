using Fdw.Configuration;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// One configured connection — the domain record, naming which connection kind it is and holding that
/// kind's own configuration.
/// </summary>
/// <remarks>
/// The interface of <c>ConnectionConfiguration</c>. The implementation contract is
/// <see cref="IConnectionImplementationConfiguration"/>.
/// </remarks>
public interface IConnectionConfiguration : IPlatformServiceConfiguration<IConnectionImplementationConfiguration>
{
}
