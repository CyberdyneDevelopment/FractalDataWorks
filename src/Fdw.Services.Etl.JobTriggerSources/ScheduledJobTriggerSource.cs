using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>The "Scheduled" job trigger source — a run started by the scheduler on its own cadence.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Scheduled")]
public sealed class ScheduledJobTriggerSource : JobTriggerSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduledJobTriggerSource"/> class.</summary>
    public ScheduledJobTriggerSource() : base(2, "Scheduled") { }
}
