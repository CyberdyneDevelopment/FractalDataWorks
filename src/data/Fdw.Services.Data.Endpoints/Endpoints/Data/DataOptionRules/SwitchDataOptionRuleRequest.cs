using FastEndpoints;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that switches a data option rule on or off (FDW-6339).</summary>
public sealed class SwitchDataOptionRuleRequest : DataOptionRuleKeyRequest
{
    /// <summary>Gets or sets the switch: the whole body of the request.</summary>
    [FromBody]
    public DataOptionRuleSwitchBody? Switch { get; set; }
}
