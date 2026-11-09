using Fdw.Collections;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>Marker interface for the options of <see cref="JobTriggerSourceTypes"/>.</summary>
public interface IJobTriggerSource : ITypeOption<int, JobTriggerSourceBase>
{
    /// <summary>Gets whether a trigger from this source must carry a per-fire key (FDW-4317): a scheduled fire does, so a retry of it can be told from a new fire; a source without one must send none.</summary>
    bool RequiresTriggerKey { get; }
}
