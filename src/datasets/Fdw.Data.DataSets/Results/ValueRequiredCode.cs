using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set request leaves out a value its caller chooses: a new source's kind, priority or predicate pushdown, a new field's
/// nullability, or a filter's operator or data type. Nothing assumes one (FDW-3931): a wrong guess would silently do the wrong thing.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "ValueRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValueRequiredCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueRequiredCode"/> class.
    /// </summary>
    public ValueRequiredCode()
        : base(20017, "ValueRequired",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: {part} '{partName}' does not state {field}, and nothing is assumed for it",
            isRetryable: false)
    {
    }
}
