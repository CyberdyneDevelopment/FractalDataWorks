using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Translator type is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "TranslatorTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TranslatorTypeRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorTypeRequiredCode"/> class.
    /// </summary>
    public TranslatorTypeRequiredCode()
        : base(21002, "TranslatorTypeRequired",
            ResultSeverities.ByName("Error"),
            "Translator type is required",
            isRetryable: false)
    {
    }
}