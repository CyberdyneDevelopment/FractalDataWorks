using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// Translation failed with an exception.
/// </summary>
[TypeOption(typeof(OpenApiResultCodes), "TranslationException", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TranslationExceptionCode : OpenApiResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public TranslationExceptionCode()
        : base(90000, "TranslationException",
            ResultSeverities.ByName("Error"),
            "Translation failed with an exception",
            isRetryable: true)
    {
    }
}