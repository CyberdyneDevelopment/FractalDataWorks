using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// The conditions a read of msg.UiFeedback is narrowed by, after the service has checked each (FDW-2143). A null condition does not narrow.
/// </summary>
internal sealed class UiFeedbackFilter
{
    /// <summary>Gets or sets the one tenant to read; null reads every tenant (the caller reaches them all).</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the state to read.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the route pattern to read.</summary>
    public string? Route { get; set; }

    /// <summary>Gets or sets the urgency to read.</summary>
    public string? Urgency { get; set; }
}
