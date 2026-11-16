using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The request of <c>GET /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/fields/{FieldName}/values</c> (FDW-5666): the
/// container and column in the route, the search and the page in the query string.
/// </summary>
public sealed class ContainerFieldValuesRequest
{
    /// <summary>Gets or sets the data store name (route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path the container is under (route).</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name (route).</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column whose values are searched (route); one of the container's discovered fields.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the text a value contains (query <c>q</c>), matched literally and ignoring case; none matches every value.</summary>
    public string? Q { get; set; }

    /// <summary>Gets or sets how many matching values to pass over (query <c>skip</c>).</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets how many values the page holds (query <c>take</c>); none takes the host's declared default.</summary>
    public int? Take { get; set; }

    /// <summary>Gets or sets the values already chosen (query <c>chosen</c>, repeated).</summary>
    public List<string> Chosen { get; set; } = [];

    /// <summary>Gets or sets whether the chosen values are left out of the page instead of marked (query <c>leaveOutChosen</c>).</summary>
    public bool LeaveOutChosen { get; set; }
}
