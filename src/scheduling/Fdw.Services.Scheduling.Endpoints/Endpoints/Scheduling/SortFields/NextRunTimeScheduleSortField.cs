using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>Sorts the schedules by the time they next run.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ScheduleSortFields), "NextRunTime")]
public sealed class NextRunTimeScheduleSortField : ScheduleSortFieldBase
{
    /// <summary>Initializes a new instance of the <see cref="NextRunTimeScheduleSortField"/> class.</summary>
    public NextRunTimeScheduleSortField()
        : base("NextRunTime", "nextruntime")
    {
    }

    /// <inheritdoc />
    public override IEnumerable<IScheduleImplementationConfiguration> Order(IEnumerable<IScheduleImplementationConfiguration> schedules, bool descending)
        => descending
            ? schedules.OrderByDescending(s => s.NextRunTime)
            : schedules.OrderBy(s => s.NextRunTime);
}
