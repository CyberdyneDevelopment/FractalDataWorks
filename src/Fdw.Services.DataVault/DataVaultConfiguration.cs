using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.DataVault;

/// <summary>
/// Header configuration class for all data vault types.
/// Generates the parent table <c>sec.DataVault</c> which contains core identity fields
/// shared by all vault types.
/// </summary>
/// <remarks>
/// <para>
/// Serves as the header row for the polymorphic vault configuration pattern:
/// <list type="bullet">
/// <item><description>As a header for <c>IOptionsMonitor&lt;List&lt;DataVaultConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>The typed body row is loaded separately by <see cref="DataVaultConfigurationProvider"/>
/// and attached to <see cref="Configuration"/> via discriminator dispatch on <see cref="Implementation"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataVault")]
public partial class DataVaultConfiguration : IDataVaultConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public DataVaultConfiguration() : this("DataVault", null, "DataVaults")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) — always "DataVault".</param>
    /// <param name="implementation">The service option type (e.g., "Default").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected DataVaultConfiguration(string serviceType, string? implementation, string sectionName)
    {
        Implementation = implementation;
    }


    /// <summary>
    /// Gets or sets the durable logical identifier (matches sec.DataVault.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this vault for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation this record names.</summary>
    [ValuesFrom(typeof(DataVaultTypes))]
    public string? Implementation { get; set; }

    /// <summary>
    /// Gets the vault type name. Alias for <see cref="Implementation"/>.
    /// </summary>
    public string? VaultType => Implementation;

    /// <summary>
    /// Gets or sets the optional description of this vault.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current (active) version of this vault configuration.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets whether this vault configuration has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the typed vault body for this header row.
    /// Populated on the read path after loading the typed body table row.
    /// Not persisted — the typed body is saved separately to its own table.
    /// </summary>
    [NotMapped]
    public IDataVaultImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IImplementationConfiguration? IDomainConfiguration.ImplementationConfiguration
    {
        get => Configuration;
        set => Configuration = (IDataVaultImplementationConfiguration?)value;
    }

}
