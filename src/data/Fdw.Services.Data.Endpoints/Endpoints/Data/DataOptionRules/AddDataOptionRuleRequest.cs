using FastEndpoints;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that adds a data option rule to a container (FDW-6339).</summary>
public sealed class AddDataOptionRuleRequest : DataOptionContainerRequest
{
    /// <summary>Gets or sets the rule to add: the whole body of the request.</summary>
    [FromBody]
    public NewDataOptionRuleBody? Rule { get; set; }
}
