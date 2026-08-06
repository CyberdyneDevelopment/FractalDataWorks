using Fdw.Configuration;
using System;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Base class for per-strategy resiliency configuration records.
/// Concrete configuration classes (PollyRetryResiliencyConfiguration, etc.)
/// inherit from this and add their strategy-specific fields.
/// </summary>
/// <remarks>
/// These are loaded from the database via <see cref="IResiliencyPolicyProvider"/>
/// and passed directly into <see cref="IResiliencyType.Execute"/>.
/// </remarks>
public class ResiliencyConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public virtual string SectionName => "Resiliency";

    /// <inheritdoc/>
    public string ServiceType => "Resiliency";

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the strategy type identifier used to select the corresponding <see cref="ResiliencyTypeBase"/> instance.
    /// </summary>
    /// <remarks>
    /// Why: StrategyType is the discriminator that maps this config to the
    /// <see cref="ResiliencyTypeBase"/> instance via <see cref="ResiliencyTypes.ByName"/>.
    /// </remarks>
    public virtual string StrategyType => string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for tenant-scoped policies.
    /// </summary>
    public Guid? TenantId { get; set; }
}
