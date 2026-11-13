using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Names one intended source of a Graph data set (FDW-5452), by data set and the source's stable id: the route of removing it.</summary>
public sealed class GraphIntendedSourceRouteRequest
{
    /// <summary>Gets or sets the name of the Graph data set.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the intended source's stable id.</summary>
    public Guid SourceId { get; set; }
}
