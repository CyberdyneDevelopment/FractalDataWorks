using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: an unanticipated error while translating an insert-from-query.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InsertFromQueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InsertFromQueryTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertFromQueryTranslationFailedCode"/> class.
    /// </summary>
    public InsertFromQueryTranslationFailedCode()
        : base(91008, "InsertFromQueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate insert-from-query: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
