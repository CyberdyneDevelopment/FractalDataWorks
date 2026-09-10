using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// Which credential service this host resolves SQL credentials through.
/// </summary>
/// <remarks>
/// The Sql implementation of the credential-store domain: it names the credential service that
/// credential operations resolve through, which is itself a row on the same store.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CredentialStore", ServiceType = "Sql")]
public sealed partial class CredentialsSqlConfiguration : ICredentialStoreImplementationConfiguration, ICredentialsSqlImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the domain record this implementation belongs to.</summary>
    public Guid CredentialStoreId { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's. The domain provider joins
    // the domain row to this one, and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the credential service credential operations resolve through.</summary>
    public string? CredentialServiceName { get; set; }
}
