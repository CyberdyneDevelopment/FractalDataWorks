using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The domain providers the summary counts kinds from that no map selection reads: connections, quality rules and schedules.</summary>
/// <remarks>
/// These are counted from the domain table alone, one query for the total, as the platform's own members: the map does not
/// draw them, so no selection holds them and a scope narrower than everything has no such count to give.
/// </remarks>
internal sealed class MapKindCounts
{
    private readonly IConnectionConfigurationProvider _connections;
    private readonly IQualityRuleConfigurationProvider _qualityRules;
    private readonly IScheduleConfigurationProvider _schedules;

    /// <summary>Initializes the providers.</summary>
    /// <param name="connections">The connection domain.</param>
    /// <param name="qualityRules">The quality rule domain.</param>
    /// <param name="schedules">The schedule domain.</param>
    public MapKindCounts(
        IConnectionConfigurationProvider connections,
        IQualityRuleConfigurationProvider qualityRules,
        IScheduleConfigurationProvider schedules)
    {
        _connections = connections;
        _qualityRules = qualityRules;
        _schedules = schedules;
    }

    /// <summary>Gets the kinds this counts, in the order a client lists them.</summary>
    public static IReadOnlyList<string> Kinds { get; } = [MapKinds.Connection, MapKinds.QualityRule, MapKinds.Schedule];

    /// <summary>Gets the domain table of a kind as a list.</summary>
    /// <param name="kind">One of <see cref="Kinds"/>.</param>
    /// <param name="logger">Records the refusal when the registered provider cannot list its domain table.</param>
    /// <returns>The domain list, or the refusal.</returns>
    public IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(string kind, ILogger logger)
        => MapSources.Records(Provider(kind) as IDomainRecordProvider<DomainConfiguration>, kind, logger);

    private object Provider(string kind) => kind switch
    {
        MapKinds.Connection => _connections,
        MapKinds.QualityRule => _qualityRules,
        _ => _schedules,
    };
}
