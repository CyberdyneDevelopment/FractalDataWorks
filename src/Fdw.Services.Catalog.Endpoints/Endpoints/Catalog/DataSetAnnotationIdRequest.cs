using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request containing a DataSet annotation identifier.</summary>
public class DataSetAnnotationIdRequest
{
    /// <summary>Gets or sets the unique identifier of the annotation.</summary>
    public Guid AnnotationId { get; set; }
}
