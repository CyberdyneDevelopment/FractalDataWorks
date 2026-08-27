using System;
using Fdw.Configuration;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Configuration for the DataGateway service.
/// </summary>
public sealed class DataGatewayConfiguration : IDataGatewayConfiguration
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

    /// <summary>
    /// Gets a value indicating whether the DataGateway is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

}
