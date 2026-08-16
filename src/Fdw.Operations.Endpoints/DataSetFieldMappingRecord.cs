using System;
using System.Collections.Generic;
using Fdw.Data;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Internal entity for DataSetFieldMapping table (lineage use).
/// </summary>
[GenerateMapper]
public class DataSetFieldMappingRecord
{
    /// <summary>Gets or sets the field mapping identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the associated DataSet source identifier.</summary>
    public Guid DataSetSourceId { get; set; }
    /// <summary>Gets or sets the logical field name in the DataSet.</summary>
    public string LogicalFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical field name in the source.</summary>
    public string PhysicalFieldName { get; set; } = string.Empty;
}