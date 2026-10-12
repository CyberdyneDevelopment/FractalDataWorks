using System;
using System.Collections.Generic;
namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request containing a DataSet name for annotation lookup.</summary>
public class DataSetAnnotationRequest
{
    /// <summary>Gets or sets the name of the DataSet.</summary>
    public string DataSetName { get; set; } = string.Empty;
}