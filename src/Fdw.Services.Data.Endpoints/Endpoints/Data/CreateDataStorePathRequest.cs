using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// One path of a data store being created, with the containers it holds.
/// </summary>
/// <remarks>
/// The request a store is created with carries its whole tree - paths, their containers, and each
/// container's fields and keys - so a store is created in one call rather than one call per
/// container (FDW-920). A path with no containers is just the path, as before.
/// </remarks>
public class CreateDataStorePathRequest
{
    /// <summary>Gets or sets the path's name within the store.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the physical path in the connection (for example a schema).</summary>
    public string PhysicalPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the path's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the containers the path holds, each with its fields and keys.</summary>
    public IList<CreateDataStorePathContainerRequest> Containers { get; set; } = [];
}
