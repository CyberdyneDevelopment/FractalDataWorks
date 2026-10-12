using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Failed to translate UPDATE command.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "UpdateTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateTranslationFailedCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTranslationFailedCode"/> class.
    /// </summary>
    public UpdateTranslationFailedCode()
        : base(91003, "UpdateTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate REST update: {ErrorMessage}",
            isRetryable: false)
    {
    }
}