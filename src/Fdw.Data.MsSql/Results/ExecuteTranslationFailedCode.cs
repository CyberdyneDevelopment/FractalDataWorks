using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: an unanticipated error while translating an execute.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "ExecuteTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteTranslationFailedCode"/> class.
    /// </summary>
    public ExecuteTranslationFailedCode()
        : base(91009, "ExecuteTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate execute: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
