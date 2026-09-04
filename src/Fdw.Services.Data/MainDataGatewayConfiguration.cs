using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Configuration for the DataGateway service.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataGateway", ServiceType = "Main")]
public partial class MainDataGatewayConfiguration : IDataGatewayImplementationConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SectionName => "DataGateway";

    /// <inheritdoc/>
    public string ServiceType => "DataGateway";

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the domain record this implementation belongs to.</summary>
    public Guid DataGatewayId { get; set; }

    /// <summary>
    /// Gets a value indicating whether the DataGateway is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the gateway caches results.
    /// </summary>
    /// <remarks>Was the DataGateway:EnableCache appsettings key. It is read from the server tier,
    /// because the gateway onto the platform store is the thing being configured and its own
    /// settings cannot live behind it.</remarks>
    public bool EnableCache { get; set; }
}
