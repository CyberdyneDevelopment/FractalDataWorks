using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// An update named a different implementation than the data set has. Each kind is its own implementation with its own table, so a data set cannot become another kind (FDW-1031). Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "ImplementationChangeNotAllowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ImplementationChangeNotAllowedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplementationChangeNotAllowedCode"/> class.
    /// </summary>
    public ImplementationChangeNotAllowedCode()
        : base(20015, "ImplementationChangeNotAllowed",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' update rejected: it is a '{implementation}' data set and cannot become '{requested}'",
            isRetryable: false)
    {
    }
}
