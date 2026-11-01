using System;
using System.Collections.Generic;

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

    /// <summary>Gets or sets the urgencies to read, any one of them; null does not narrow by this (FDW-2246).</summary>
    public IReadOnlyList<string>? Urgencies { get; set; }

    /// <summary>Gets or sets the one author whose comments to read; null reads every author (FDW-2246).</summary>
    public Guid? AuthorUserId { get; set; }

    /// <summary>Makes an independent copy, to narrow further without changing this one.</summary>
    /// <returns>The copy.</returns>
    public UiFeedbackFilter Copy() => new()
    {
        TenantId = TenantId,
        State = State,
        Route = Route,
        Urgency = Urgency,
        Urgencies = Urgencies,
        AuthorUserId = AuthorUserId
    };
}
