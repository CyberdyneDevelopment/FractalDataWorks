using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Failed to fetch OpenAPI specification.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiSpecFetchFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiSpecFetchFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSpecFetchFailedCode"/> class.
    /// </summary>
    public OpenApiSpecFetchFailedCode()
        : base(71001, "OpenApiSpecFetchFailed",
            ResultSeverities.ByName("Error"),
            "Failed to fetch OpenAPI spec from '{Source}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}