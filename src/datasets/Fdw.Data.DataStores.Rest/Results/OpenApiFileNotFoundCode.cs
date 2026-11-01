using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// File not found for OpenAPI specification.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiFileNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiFileNotFoundCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiFileNotFoundCode"/> class.
    /// </summary>
    public OpenApiFileNotFoundCode()
        : base(30000, "OpenApiFileNotFound",
            ResultSeverities.ByName("Error"),
            "File not found: {FilePath}",
            isRetryable: false)
    {
    }
}