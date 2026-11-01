using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every EmptyBody implementation's configuration satisfies -- the routes whose requests may arrive without a body.
/// </summary>
public interface IEmptyBodyImplementationConfiguration : IImplementationConfiguration
{
}
