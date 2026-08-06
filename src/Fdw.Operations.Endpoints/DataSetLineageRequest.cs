using System;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Request for lineage of a specific DataSet.
/// </summary>
public class DataSetLineageRequest
{
    /// <summary>Gets or sets the DataSet name to retrieve lineage for.</summary>
    public string Name { get; set; } = string.Empty;
}