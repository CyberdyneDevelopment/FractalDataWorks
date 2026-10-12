using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Command is missing required input data.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "MissingInputData", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingInputDataCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingInputDataCode"/> class.
    /// </summary>
    public MissingInputDataCode()
        : base(21001, "MissingInputData",
            ResultSeverities.ByName("Error"),
            "{CommandType} must have input data",
            isRetryable: false)
    {
    }
}