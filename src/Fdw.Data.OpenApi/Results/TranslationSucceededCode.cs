using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// Translation completed successfully.
/// </summary>
[TypeOption(typeof(OpenApiResultCodes), "TranslationSucceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TranslationSucceededCode : OpenApiResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public TranslationSucceededCode()
        : base(10000, "TranslationSucceeded",
            ResultSeverities.ByName("Success"),
            "OpenAPI translation completed successfully",
            isRetryable: false)
    {
    }
}