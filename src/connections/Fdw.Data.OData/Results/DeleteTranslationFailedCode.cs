using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Failed to translate DELETE command.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "DeleteTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteTranslationFailedCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTranslationFailedCode"/> class.
    /// </summary>
    public DeleteTranslationFailedCode()
        : base(91000, "DeleteTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate REST delete: {ErrorMessage}",
            isRetryable: false)
    {
    }
}