using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Failed to create OpenAPI endpoint path.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiEndpointPathFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiEndpointPathFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiEndpointPathFailedCode"/> class.
    /// </summary>
    public OpenApiEndpointPathFailedCode()
        : base(91000, "OpenApiEndpointPathFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create endpoint path for '{Method} {PathTemplate}': {ErrorMessage}",
            isRetryable: false)
    {
    }
}