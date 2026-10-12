using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>Base class for job trigger sources — what caused an ETL job to run.</summary>
[ExcludeFromCodeCoverage]
public abstract class JobTriggerSourceBase : TypeOptionBase<JobTriggerSourceBase>, IJobTriggerSource
{
    /// <summary>Initializes a new instance of the <see cref="JobTriggerSourceBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected JobTriggerSourceBase(string name) : base(name) { }
}
