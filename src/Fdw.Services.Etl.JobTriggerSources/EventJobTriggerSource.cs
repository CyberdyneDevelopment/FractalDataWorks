using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>The "Event" job trigger source — a run started by an event arriving.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Event")]
public sealed class EventJobTriggerSource : JobTriggerSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="EventJobTriggerSource"/> class.</summary>
    public EventJobTriggerSource() : base(3, "Event") { }
}
