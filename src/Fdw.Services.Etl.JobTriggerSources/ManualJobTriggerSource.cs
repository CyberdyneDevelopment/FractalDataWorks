using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>The "Manual" job trigger source — a run started by a person, on demand.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Manual")]
public sealed class ManualJobTriggerSource : JobTriggerSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="ManualJobTriggerSource"/> class.</summary>
    public ManualJobTriggerSource() : base(1, "Manual") { }
}
