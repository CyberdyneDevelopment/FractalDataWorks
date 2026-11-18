using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A request addressed to one data option rule of a container (FDW-6339).</summary>
public class DataOptionRuleKeyRequest : DataOptionContainerRequest
{
    /// <summary>Gets or sets the rule's id (route).</summary>
    public Guid RuleId { get; set; }
}
