using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that lists the recorded changes of a container's data option rules (FDW-6339).</summary>
public sealed class ListDataOptionRuleHistoryRequest : DataOptionContainerRequest
{
    /// <summary>Gets or sets the column whose rules' changes to list (body).</summary>
    public string? Column { get; set; }

    /// <summary>Gets or sets the rule whose changes to list (body).</summary>
    public Guid? RuleId { get; set; }

    /// <summary>Gets or sets the page, from 1 (body).</summary>
    public int? Page { get; set; }

    /// <summary>Gets or sets the page size (body).</summary>
    public int? PageSize { get; set; }
}
