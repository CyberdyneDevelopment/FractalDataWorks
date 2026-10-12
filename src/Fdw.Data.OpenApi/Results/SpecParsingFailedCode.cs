using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// OpenAPI specification parsing failed.
/// </summary>
[TypeOption(typeof(OpenApiResultCodes), "SpecParsingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SpecParsingFailedCode : OpenApiResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public SpecParsingFailedCode()
        : base(90003, "SpecParsingFailed",
            ResultSeverities.ByName("Error"),
            "Failed to parse OpenAPI specification: {Reason}",
            isRetryable: false)
    {
    }
}