using System;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One kind of node the map knows, and what the map does with an object of that kind.</summary>
public interface IMapKindOption : ITypeOption<int, MapKindOptionBase>
{
    /// <summary>Gets the id the map's node for an object of this kind carries.</summary>
    /// <param name="id">The object's logical id.</param>
    /// <returns>The node id.</returns>
    string NodeId(Guid id);

    /// <summary>Gets the domain table of this kind as a list.</summary>
    /// <param name="providers">The domain providers.</param>
    /// <param name="logger">Records the refusal when the registered provider cannot list its domain table.</param>
    /// <returns>The domain list, or the refusal: a kind the summary does not count from a domain table has no list to give.</returns>
    IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(MapCountProviders providers, ILogger logger);
}
