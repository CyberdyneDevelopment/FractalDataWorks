using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Failed to translate INSERT command.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "InsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InsertTranslationFailedCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertTranslationFailedCode"/> class.
    /// </summary>
    public InsertTranslationFailedCode()
        : base(91001, "InsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate REST insert: {ErrorMessage}",
            isRetryable: false)
    {
    }
}