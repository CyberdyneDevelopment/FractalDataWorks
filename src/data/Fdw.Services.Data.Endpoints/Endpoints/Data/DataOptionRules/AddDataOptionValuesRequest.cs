using FastEndpoints;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that adds values to a data option rule (FDW-6339).</summary>
public sealed class AddDataOptionValuesRequest : DataOptionRuleKeyRequest
{
    /// <summary>Gets or sets the values to add: the whole body of the request.</summary>
    [FromBody]
    public DataOptionValuesBody? Values { get; set; }
}
