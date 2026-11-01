using System;
using Fdw.Data;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// A data set as its domain table holds it: the identity and the name, which is all <c>data.DataSet</c> stores. What the dataflow
/// graph shows of a data set beyond that (version, description, record type, category) is read from the data set itself.
/// </summary>
[GenerateMapper]
public partial class DataSetRecord
{
    /// <summary>Gets or sets the data set identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;
}
