using Fdw.Configuration;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// The contract every credential-store implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// A credential store answers "where does this host resolve credentials from". Implementations
/// differ in the store they reach -- the Sql implementation names a credential service row -- and
/// the domain configuration holds one of these, named by its <c>Implementation</c>.
/// </remarks>
public interface ICredentialStoreImplementationConfiguration : IImplementationConfiguration
{
}
