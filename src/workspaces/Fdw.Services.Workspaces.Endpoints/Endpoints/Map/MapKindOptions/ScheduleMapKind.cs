using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A schedule, counted from its domain table by the summary.</summary>
[TypeOption(typeof(MapKindOptions), "Schedule")]
[ExcludeFromCodeCoverage]
public sealed class ScheduleMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleMapKind"/> class.</summary>
    public ScheduleMapKind()
        : base("Schedule")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(MapCountProviders providers, ILogger logger)
        => MapSources.Records(providers.Schedules as IDomainRecordProvider<DomainConfiguration>, Name, logger);
}
