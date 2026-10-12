using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Delete translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "DeleteTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTranslationFailedCode"/> class.
    /// </summary>
    public DeleteTranslationFailedCode()
        : base(91003, "DeleteTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate delete: {ErrorMessage}",
            isRetryable: false)
    {
    }
}