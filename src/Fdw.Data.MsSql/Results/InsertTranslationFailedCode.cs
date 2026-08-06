using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Insert translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InsertTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertTranslationFailedCode"/> class.
    /// </summary>
    public InsertTranslationFailedCode()
        : base(91005, "InsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate insert: {ErrorMessage}",
            isRetryable: false)
    {
    }
}