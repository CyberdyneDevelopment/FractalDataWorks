using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The option rows an add stored (FDW-5664), each with its id.</summary>
public sealed class DataOptionRowsPayload
{
    /// <summary>Gets or sets the rows, in the order the add named them.</summary>
    public List<DataOptionRowPayload> Items { get; set; } = [];
}
