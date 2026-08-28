using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Credentials.Abstractions;

namespace Fdw.Services.Credentials;

/// <summary>
/// Header configuration class for all credential service types.
/// Generates the parent table <c>sec.CredentialService</c> which contains core identity fields
/// shared by all credential service types.
/// </summary>
/// <remarks>
/// <para>
/// Serves as the header row for the polymorphic credential service configuration pattern:
/// <list type="bullet">
/// <item><description>As a header for <c>IOptionsMonitor&lt;List&lt;CredentialServiceConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>The typed body row is loaded separately by <see cref="CredentialServiceConfigurationProvider"/>
/// and attached to <see cref="Configuration"/> via discriminator dispatch on <see cref="ServiceOptionType"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CredentialService")]
public partial class CredentialServiceConfiguration : ICredentialServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public CredentialServiceConfiguration() : this("CredentialService", null, "CredentialServices")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) — always "CredentialService".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "Sql").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected CredentialServiceConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }


    /// <summary>
    /// Gets or sets the durable logical identifier (matches sec.CredentialService.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this credential service for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) — always "CredentialService" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (e.g., "Sql").
    /// </summary>
    [ValuesFrom(typeof(CredentialServiceTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the credential service type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public string? CredentialServiceType => ServiceOptionType;

    /// <summary>
    /// Gets or sets the optional description of this credential service.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current (active) version of this credential service configuration.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets whether this credential service configuration has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the typed credential service body for this header row.
    /// Populated on the read path after loading the typed body table row.
    /// Not persisted — the typed body is saved separately to its own table.
    /// </summary>
    [NotMapped]
    public ICredentialServiceImplementationConfiguration? Configuration { get; set; }
}
