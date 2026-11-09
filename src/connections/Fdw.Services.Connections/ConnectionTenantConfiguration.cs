using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections;

/// <summary>
/// The part of a <c>conn.Connection</c> row that says whose connection it is (FDW-4192): its Id, its tenant and its visibility group.
/// </summary>
/// <remarks>
/// Read only, and only to stamp the disabled state of a connection with the connection's own tenant, so that tenant's reads
/// see the state however the writer was signed in. The connection provider does not return the tenant, because a domain row
/// carries identity and nothing else.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ConnectionTenantConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the Id of the connection's domain row.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the connection's own tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the visibility group of the connection's row; the state of the connection takes the same group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the newest version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been deleted.</summary>
    public bool IsDeleted { get; set; }
}
