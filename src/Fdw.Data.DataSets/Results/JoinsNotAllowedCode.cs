using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// Joins were supplied for a data set whose kind has none: only Compound and Federated data sets join their sources (FDW-1031). Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "JoinsNotAllowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JoinsNotAllowedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinsNotAllowedCode"/> class.
    /// </summary>
    public JoinsNotAllowedCode()
        : base(20013, "JoinsNotAllowed",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: joins are not allowed when implementation is '{implementation}'",
            isRetryable: false)
    {
    }
}
