using Fdw.Configuration;

namespace Fdw.Services.Multitenancy;

/// <summary>The contract every Multitenancy implementation carries.</summary>
/// <remarks>
/// What a multitenancy implementation needs is its own -- the Sql one names the store and the
/// tables it reads tenants from -- so the domain's contract adds nothing of its own.
/// </remarks>
public interface IMultitenancyImplementationConfiguration : IImplementationConfiguration
{
}
