using System;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// The contract every CredentialsSql implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be
/// registered against this domain or handed back by a read of it.
/// </remarks>
public interface ICredentialsSqlImplementationConfiguration : ICredentialStoreImplementationConfiguration
{

    /// <summary>Gets or sets the name of the credential service whose store this configures.</summary>
    string? CredentialServiceName { get; set; }

    /// <summary>Gets or sets the domain record's durable id.</summary>
    Guid CredentialStoreId { get; set; }
}
