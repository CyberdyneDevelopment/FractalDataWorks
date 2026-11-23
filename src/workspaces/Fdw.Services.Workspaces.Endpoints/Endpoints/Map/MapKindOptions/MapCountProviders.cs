using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The domain providers a map kind can list the rows of its domain table from.</summary>
/// <remarks>Public only because <see cref="IMapKindOption.Records"/> takes it; nothing outside the map reads its members.</remarks>
/// <param name="Connections">The connection domain.</param>
/// <param name="QualityRules">The quality rule domain.</param>
/// <param name="Schedules">The schedule domain.</param>
public sealed record MapCountProviders(
    IConnectionConfigurationProvider Connections,
    IQualityRuleConfigurationProvider QualityRules,
    IScheduleConfigurationProvider Schedules);
