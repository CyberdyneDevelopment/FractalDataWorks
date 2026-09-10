using System;
using System.ComponentModel.DataAnnotations.Schema;
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
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

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
