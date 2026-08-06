using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// OpenAPI spec URL or path was null or empty.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiSpecRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiSpecRequiredCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSpecRequiredCode"/> class.
    /// </summary>
    public OpenApiSpecRequiredCode()
        : base(21001, "OpenApiSpecRequired",
            ResultSeverities.ByName("Error"),
            "OpenAPI spec URL or path cannot be null or empty",
            isRetryable: false)
    {
    }
}