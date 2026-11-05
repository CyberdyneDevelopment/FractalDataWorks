using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What one load step of a graph destination does: Insert, Upsert, Update, Delete or Truncate (FDW-1108).
/// </summary>
public interface ILoadStepKind : ITypeOption<int, ILoadStepKind>, ILoadOptionShape, IAcceptsRejectedRecordsShape
{
    /// <summary>Gets whether the step reads its rows from a source data set of its own, with an optional filter expression.</summary>
    bool UsesSourceDataSet { get; }

    /// <summary>Gets whether the step works on the pipeline's transformed output rows.</summary>
    bool UsesRows { get; }

    /// <summary>
    /// Gets whether running the step again over the same rows leaves the target as the first run did (FDW-3840): an upsert, an update, a delete and a truncate do, an insert
    /// adds the rows again. A run that is cut off is started again only when each destination that committed rows repeats to the same result.
    /// </summary>
    bool RepeatsToSameResult { get; }
}
