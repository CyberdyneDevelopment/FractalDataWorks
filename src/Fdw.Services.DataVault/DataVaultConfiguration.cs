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
public partial class DataVaultConfiguration : DomainConfigurationBase, IDataVaultConfiguration
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










}
