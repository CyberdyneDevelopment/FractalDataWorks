using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What one load step of a graph destination does: BulkInsert, BulkUpsert, InsertFromQuery, Update, Delete, Truncate or Execute (FDW-1108).
/// </summary>
public interface ILoadStepKind : ITypeOption<int, ILoadStepKind>, ILoadOptionShape, IAcceptsRejectedRecordsShape
{
    /// <summary>Gets whether the step reads its rows from a source data set of its own, with an optional filter expression.</summary>
    bool UsesSourceDataSet { get; }

    /// <summary>Gets whether the step runs a named executable container of its destination data store.</summary>
    bool UsesDestinationContainer { get; }

    /// <summary>Gets whether the step works on the pipeline's transformed output rows.</summary>
    bool UsesRows { get; }
}
