using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A Graph data set update named a data set that is not a Graph data set. A data set's Simple, Compound or Federated kind follows its sources
/// (FDW-6806) and is never named by a caller; turning one into a Graph data set is the conversion route's (FDW-6972), never a graph update.
/// Caller-input validation failure: HTTP 400 (Validation category).
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
