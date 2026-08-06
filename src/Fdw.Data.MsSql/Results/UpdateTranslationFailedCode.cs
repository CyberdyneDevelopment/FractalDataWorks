using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Update translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "UpdateTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTranslationFailedCode"/> class.
    /// </summary>
    public UpdateTranslationFailedCode()
        : base(91006, "UpdateTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate update: {ErrorMessage}",
            isRetryable: false)
    {
    }
}