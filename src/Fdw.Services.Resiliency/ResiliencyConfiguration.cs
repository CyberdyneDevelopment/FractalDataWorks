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
public class ResiliencyConfiguration : IResiliencyConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets the strategy type identifier used to select the corresponding <see cref="ResiliencyTypeBase"/> instance.
    /// </summary>
    /// <remarks>
    /// Why: Implementation is the discriminator that maps this config to the
    /// <see cref="ResiliencyTypeBase"/> instance via <see cref="ResiliencyTypes.ByName"/>.
    /// </remarks>
    public virtual string Implementation => string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for tenant-scoped policies.
    /// </summary>
    public Guid? TenantId { get; set; }
}
