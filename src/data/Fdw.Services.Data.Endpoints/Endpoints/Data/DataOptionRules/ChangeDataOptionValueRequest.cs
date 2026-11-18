using System;
using FastEndpoints;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that changes one value of a data option rule (FDW-6339).</summary>
public sealed class ChangeDataOptionValueRequest : DataOptionRuleKeyRequest
{
    /// <summary>Gets or sets the value's id (route).</summary>
    public Guid ValueId { get; set; }

    /// <summary>Gets or sets the change: the whole body of the request.</summary>
    [FromBody]
    public DataOptionValueChangeBody? Change { get; set; }
}
