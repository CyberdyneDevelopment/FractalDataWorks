using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Command is missing required input data.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "MissingInputData", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingInputDataCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingInputDataCode"/> class.
    /// </summary>
    public MissingInputDataCode()
        : base(20000, "MissingInputData",
            ResultSeverities.ByName("Error"),
            "Command requires input data but none was provided. CommandType: {CommandType}",
            isRetryable: false)
    {
    }
}
