using Fdw.Configuration;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>The contract every organization implementation carries.</summary>
/// <remarks>
/// An organization's fields are the ones its list shows, and they live on the domain row
/// (<see cref="OrganizationDomainConfiguration"/>). The implementation row holds only its key to it, as a
/// permission's does since FDW-1002.
/// </remarks>
public interface IOrganizationImplementationConfiguration : IImplementationConfiguration
{
}
