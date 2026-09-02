using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// Which credential service this host resolves SQL credentials through.
/// </summary>
/// <remarks>
/// Was the CredentialsSql appsettings section. The selector only -- no policy -- and the service it
/// names is itself a row, so the selection belongs on the same store rather than in a file.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Credentials", ServiceType = "CredentialsSql")]
public sealed partial class CredentialsSqlConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    string IGenericConfiguration.SectionName => "Credentials";

    string IGenericConfiguration.ServiceType => "Credentials";

    string? IGenericConfiguration.ServiceOptionType => "CredentialsSql";

    /// <summary>Gets or sets the credential service credential operations resolve through.</summary>
    public string? CredentialServiceName { get; set; }
}
