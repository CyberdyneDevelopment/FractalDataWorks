using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Configuration;

/// <summary>
/// A connection a platform administrator grants a tenant for a purpose (FDW-1581), optionally narrowed to a folder.
/// </summary>
/// <remarks>
/// Persisted to <c>tenant.ConnectionGrantConfiguration</c>, reached through the domain row in <c>tenant.ConnectionGrant</c>, whose name is
/// <c>{GranteeTenantId}:{Purpose}:{ConnectionName}</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ConnectionGrant")]
public partial class ConnectionGrantImplementationConfiguration : IConnectionGrantImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid GranteeTenantId { get; set; }

    /// <inheritdoc />
    public string ConnectionName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Purpose { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? FolderPath { get; set; }

    /// <inheritdoc />
    public bool MayRegisterOwn { get; set; }

    /// <inheritdoc />
    public bool IsCurrent { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }
}
