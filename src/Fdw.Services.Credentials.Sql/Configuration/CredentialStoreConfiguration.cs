using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// The credential-store domain: which store this host resolves credentials through.
/// </summary>
/// <remarks>
/// CredentialsSql used to be a selector row with no domain above it -- it named a credential
/// service and nothing named it, so there was no record to resolve by name and no place for a
/// second store to be configured. It is an implementation of this.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CredentialStore")]
public partial class CredentialStoreConfiguration : IDomainConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain => "CredentialStore";

    /// <summary>Gets or sets the implementation this record names.</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the implementation's own configuration.</summary>
    public ICredentialStoreImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    IGenericConfiguration? IDomainConfiguration.ImplementationConfiguration => Configuration;
}
