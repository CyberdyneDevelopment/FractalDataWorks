using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The base of every kind of node the map knows.</summary>
[ExcludeFromCodeCoverage]
public abstract class MapKindOptionBase : TypeOptionBase<MapKindOptionBase>, IMapKindOption
{
    /// <summary>Initializes a new instance of the <see cref="MapKindOptionBase"/> class.</summary>
    /// <param name="name">The kind, as the string a node carries.</param>
    protected MapKindOptionBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public string NodeId(Guid id) => MapNodeIds.Other(Name, id);

    /// <inheritdoc />
    /// <remarks>A kind with no domain table of its own to list answers <c>MapSearchUnavailable</c>; the kinds the summary counts override this.</remarks>
    public virtual IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(MapCountProviders providers, ILogger logger)
        => MapSources.Records(null, Name, logger);
}
