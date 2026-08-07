using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>Base class for job trigger sources — what caused an ETL job to run.</summary>
[ExcludeFromCodeCoverage]
public abstract class JobTriggerSourceBase : TypeOptionBase<int, JobTriggerSourceBase>, IJobTriggerSource
{
    /// <summary>Initializes a new instance of the <see cref="JobTriggerSourceBase"/> class.</summary>
    /// <param name="id">The option's identifier.</param>
    /// <param name="name">The option's name.</param>
    protected JobTriggerSourceBase(int id, string name) : base(id, name) { }
}
