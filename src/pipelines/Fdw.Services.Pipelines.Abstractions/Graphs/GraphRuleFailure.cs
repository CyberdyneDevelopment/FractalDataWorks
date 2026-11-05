using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One failure that counted toward a pipeline's failure threshold (FDW-3864).</summary>
public sealed class GraphRuleFailure
{
    /// <summary>Gets or sets the run.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets when the run ended.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets the failure's category name; null when it carried none.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the failure's code; null when it carried none.</summary>
    public string? Code { get; set; }
}
