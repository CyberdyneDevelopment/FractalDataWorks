using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The longest text each field of a rule and of a value takes (FDW-6339), read from the containers' declared fields.</summary>
public sealed class DataOptionLimitsPayload
{
    /// <summary>Gets or sets each rule field's maximum length, by field name.</summary>
    public Dictionary<string, int> Rule { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets each value field's maximum length, by field name.</summary>
    public Dictionary<string, int> Value { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets every changeable member of a rule and of a value, with its display name and whether it may be cleared.</summary>
    public List<DataOptionMemberPayload> Members { get; set; } = [];
}
