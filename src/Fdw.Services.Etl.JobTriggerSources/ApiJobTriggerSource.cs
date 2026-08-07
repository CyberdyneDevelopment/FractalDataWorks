using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>The "Api" job trigger source — a run started by an inbound API call.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Api")]
public sealed class ApiJobTriggerSource : JobTriggerSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="ApiJobTriggerSource"/> class.</summary>
    public ApiJobTriggerSource() : base(4, "Api") { }
}
