using System;
using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Groups all fields for a single DataSet, used in bulk field enumeration for formula autocomplete.
/// </summary>
public sealed class DataSetFieldsPayload
{
    /// <summary>
    /// Gets or sets the name of the DataSet.
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fields belonging to this DataSet.
    /// </summary>
    public IReadOnlyList<FieldInfoPayload> Fields { get; set; } = Array.Empty<FieldInfoPayload>();
}
