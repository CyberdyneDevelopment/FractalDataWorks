using System;
using System.Collections.Generic;
namespace Fdw.Services.Quality.Endpoints;

/// <summary>Request containing an optional DataSet name for filtering queries.</summary>
public class DataSetQueryRequest
{
    /// <summary>Gets or sets the DataSet name to filter by.</summary>
    public string? DataSetName { get; set; }
}