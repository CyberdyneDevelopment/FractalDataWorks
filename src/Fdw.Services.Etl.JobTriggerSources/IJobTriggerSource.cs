using Fdw.Collections;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>Marker interface for the options of <see cref="JobTriggerSourceTypes"/>.</summary>
public interface IJobTriggerSource : ITypeOption<int, JobTriggerSourceBase>
{
    // Marker interface for now
}
