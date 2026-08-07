using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>The job trigger sources an ETL job can be started by.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(JobTriggerSourceBase), typeof(IJobTriggerSource), typeof(JobTriggerSourceTypes))]
public abstract partial class JobTriggerSourceTypes : TypeCollectionBase<JobTriggerSourceBase, IJobTriggerSource>
{
}
