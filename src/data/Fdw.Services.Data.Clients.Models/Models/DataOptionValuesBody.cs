using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The body of values to add to a data option rule (FDW-6339).</summary>
public sealed class DataOptionValuesBody
{
    /// <summary>Gets or sets the values to add, all or none.</summary>
    public List<DataOptionValueBody>? Values { get; set; }
}
