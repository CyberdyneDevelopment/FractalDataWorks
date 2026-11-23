using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>Sorts the schedules by name, compared ordinally.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ScheduleSortFields), "ScheduleName")]
public sealed class NameScheduleSortField : ScheduleSortFieldBase
{
    /// <summary>Initializes a new instance of the <see cref="NameScheduleSortField"/> class.</summary>
    public NameScheduleSortField()
        : base("ScheduleName", "name")
    {
    }

    /// <inheritdoc />
    public override IEnumerable<IScheduleImplementationConfiguration> Order(IEnumerable<IScheduleImplementationConfiguration> schedules, bool descending)
        => descending
            ? schedules.OrderByDescending(s => s.Name, StringComparer.Ordinal)
            : schedules.OrderBy(s => s.Name, StringComparer.Ordinal);
}
