using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that removes values of a data option rule, keeping them (FDW-6339).</summary>
public sealed class RemoveDataOptionValuesRequest : DataOptionRuleKeyRequest
{
    /// <summary>Gets or sets the ids of the values to remove (query id, repeated), all or none.</summary>
    public List<Guid> Id { get; set; } = [];
}
