using Fdw.Configuration;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// The configuration a data gateway is resolved against.
/// </summary>
/// <remarks>
/// It lives here rather than beside its class because a contract in this package cannot name a type in
/// the core package; the dependency runs the other way. Declaring it is what lets the domain's provider
/// contract name its configuration at all.
/// </remarks>
public interface IDataGatewayConfiguration : IGenericConfiguration
{
}
