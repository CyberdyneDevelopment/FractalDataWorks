using Fdw.Configuration;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The contract every role-mapping implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// A role mapping answers "which role names carry which authority". Implementations differ in whose
/// authority they describe — the System implementation names the roles that carry system authority —
/// and the domain configuration holds one of these, named by its <c>ServiceOptionType</c>.
/// </remarks>
public interface IRoleMappingImplementationConfiguration : IImplementationConfiguration
{
}
