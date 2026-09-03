using System;
using Fdw.Configuration;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Configuration for the DataGateway service.
/// </summary>
public sealed class MainDataGatewayConfiguration : IDataGatewayImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; } = Guid.CreateVersion7();

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
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the gateway caches results.
    /// </summary>
    /// <remarks>Was the DataGateway:EnableCache appsettings key. It is read from the server tier,
    /// because the gateway onto the platform store is the thing being configured and its own
    /// settings cannot live behind it.</remarks>
    public bool EnableCache { get; set; }
}
