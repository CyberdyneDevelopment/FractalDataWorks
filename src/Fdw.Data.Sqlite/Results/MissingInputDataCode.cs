using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Command has no input data.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "MissingInputData", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingInputDataCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingInputDataCode"/> class.
    /// </summary>
    public MissingInputDataCode()
        : base(21004, "MissingInputData",
            ResultSeverities.ByName("Error"),
            "Command has no input data",
            isRetryable: false)
    {
    }
}
