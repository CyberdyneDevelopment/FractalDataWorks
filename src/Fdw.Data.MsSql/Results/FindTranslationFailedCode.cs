using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Find translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "FindTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FindTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTranslationFailedCode"/> class.
    /// </summary>
    public FindTranslationFailedCode()
        : base(91004, "FindTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate find command: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
