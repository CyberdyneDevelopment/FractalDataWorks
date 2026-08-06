using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Failed to parse OpenAPI specification.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiParsingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiParsingFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiParsingFailedCode"/> class.
    /// </summary>
    public OpenApiParsingFailedCode()
        : base(91001, "OpenApiParsingFailed",
            ResultSeverities.ByName("Error"),
            "Failed to parse OpenAPI spec: {ErrorMessage}",
            isRetryable: false)
    {
    }
}