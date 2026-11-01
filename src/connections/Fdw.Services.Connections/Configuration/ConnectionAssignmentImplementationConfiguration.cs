using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Configuration;

/// <summary>
/// The connection a tenant administrator assigns to an organization for a purpose (FDW-1581), one of the tenant's own grants.
/// </summary>
/// <remarks>
/// Persisted to <c>tenant.ConnectionAssignmentConfiguration</c>, reached through the domain row in <c>tenant.ConnectionAssignment</c>, whose name is
/// <c>{OrganizationId}:{Purpose}</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ConnectionAssignment")]
public partial class ConnectionAssignmentImplementationConfiguration : IConnectionAssignmentImplementationConfiguration
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
    public Guid OrganizationId { get; set; }

    /// <inheritdoc />
    public string Purpose { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid GrantId { get; set; }

    /// <inheritdoc />
    public string? BaseFolder { get; set; }

    /// <inheritdoc />
    public bool IsCurrent { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }
}
