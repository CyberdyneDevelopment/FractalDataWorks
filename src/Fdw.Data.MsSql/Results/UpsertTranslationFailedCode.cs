using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: an unanticipated error while translating an upsert.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "UpsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpsertTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertTranslationFailedCode"/> class.
    /// </summary>
    public UpsertTranslationFailedCode()
        : base(91007, "UpsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate upsert: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
