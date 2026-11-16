using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One page of the distinct values of a container column (FDW-5666).</summary>
public sealed class ContainerFieldValuesPayload
{
    /// <summary>Gets or sets the values on this page, in value order.</summary>
    public List<ContainerFieldValuePayload> Items { get; set; } = [];

    /// <summary>Gets or sets whether more values match beyond this page.</summary>
    public bool HasMore { get; set; }
}
