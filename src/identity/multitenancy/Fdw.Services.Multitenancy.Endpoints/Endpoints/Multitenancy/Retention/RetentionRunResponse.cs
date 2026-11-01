using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// What one run of the retention job found and did for an organization (FDW-1575).
/// </summary>
public class RetentionRunResponse
{
    /// <summary>Gets or sets the moment the run judged every document against.</summary>
    public DateTimeOffset RanAt { get; set; }

    /// <summary>Gets or sets how many documents of the types with a policy the run looked at.</summary>
    public int Considered { get; set; }

    /// <summary>Gets or sets how many were past their deadline but under a legal hold.</summary>
    public int Held { get; set; }

    /// <summary>Gets or sets how many lack the date their policy counts from.</summary>
    public int Undated { get; set; }

    /// <summary>Gets or sets the documents the run ended.</summary>
    public IReadOnlyList<RetentionOutcomeResponse> Ended { get; set; } = [];

    /// <summary>Gets or sets the documents the run could not end.</summary>
    public IReadOnlyList<RetentionOutcomeResponse> Failed { get; set; } = [];
}
