using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>Sorts the schedules by the name of the pipeline they run, compared ordinally.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ScheduleSortFields), "PipelineName")]
public sealed class PipelineNameScheduleSortField : ScheduleSortFieldBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineNameScheduleSortField"/> class.</summary>
    public PipelineNameScheduleSortField()
        : base("PipelineName", "pipelinename")
    {
    }

    /// <inheritdoc />
    public override IEnumerable<IScheduleImplementationConfiguration> Order(IEnumerable<IScheduleImplementationConfiguration> schedules, bool descending)
        => descending
            ? schedules.OrderByDescending(s => s.PipelineName, StringComparer.Ordinal)
            : schedules.OrderBy(s => s.PipelineName, StringComparer.Ordinal);
}
