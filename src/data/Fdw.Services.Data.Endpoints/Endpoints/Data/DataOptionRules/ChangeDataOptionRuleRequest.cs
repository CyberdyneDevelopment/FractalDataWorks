using FastEndpoints;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that changes a data option rule (FDW-6339).</summary>
public sealed class ChangeDataOptionRuleRequest : DataOptionRuleKeyRequest
{
    /// <summary>Gets or sets the change: the whole body of the request.</summary>
    [FromBody]
    public DataOptionRuleChangeBody? Change { get; set; }
}
