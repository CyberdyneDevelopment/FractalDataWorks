using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections;

/// <summary>
/// The row of <c>conn.ConnectionDisabled</c> (FDW-3951): the state of one connection, versioned on write so that
/// disabling and enabling it again leave a history.
/// </summary>
/// <remarks>
/// A connection is disabled when its current row has no <see cref="EnabledAt"/>. Enabling it saves a new version carrying
/// <see cref="EnabledAt"/> and <see cref="EnabledReason"/>; disabling it again saves another. The row holds the connection's
/// own tenant, so that tenant's reads see it however the writer was signed in.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ConnectionDisabledConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the durable Id of the state, kept by every version of it.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the Id of the connection's domain row.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets when the connection was disabled.</summary>
    public DateTimeOffset DisabledAt { get; set; }

    /// <summary>Gets or sets why the connection was disabled.</summary>
    public string DisabledReason { get; set; } = string.Empty;

    /// <summary>Gets or sets who or what disabled the connection.</summary>
    public string DisabledBy { get; set; } = string.Empty;

    /// <summary>Gets or sets when a person enabled the connection again; not set while it is disabled.</summary>
    public DateTimeOffset? EnabledAt { get; set; }

    /// <summary>Gets or sets why the connection was enabled again; not set while it is disabled.</summary>
    public string? EnabledReason { get; set; }

    /// <summary>Gets or sets the connection's own tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the visibility group of the row.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the newest version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets when the row was created at its source.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets when this version was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created this version.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets on whose behalf this version was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets when this version was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who last modified this version.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets on whose behalf this version was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
