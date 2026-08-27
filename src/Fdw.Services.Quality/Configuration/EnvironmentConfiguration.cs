using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for deployment environments.
/// Stored in quality.Environment table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Promotion",
    ServiceType = "Environment")]
// Why: IGenericConfiguration is required by ImplementationConfigurationProviderBase<T>
// for dual-source (ctrl+cfg) provider pattern.
public sealed partial class EnvironmentConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Promotions";

    /// <inheritdoc />
    // Why: Matches ServiceCategory from [ManagedConfiguration] attribute for IOptions binding path.
    public string ServiceType => "Promotion";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the unique identifier for this environment.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name of the environment (e.g., "Development", "Staging", "Production").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order in the promotion pipeline (lower values promote first).
    /// </summary>
    public int PromotionOrder { get; set; }

    /// <summary>
    /// Gets or sets the connection name for this environment.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this environment requires approval for promotions.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Gets or sets the collection of approvers for this environment.
    /// </summary>
    public IList<EnvironmentApproverConfiguration> Approvers { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional description of this environment.
    /// </summary>
    public string? Description { get; set; }
}
