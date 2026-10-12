using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// OpenAPI import operation failed.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "OpenApiImportFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OpenApiImportFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiImportFailedCode"/> class.
    /// </summary>
    public OpenApiImportFailedCode()
        : base(71000, "OpenApiImportFailed",
            ResultSeverities.ByName("Error"),
            "OpenAPI schema import failed",
            isRetryable: true)
    {
    }
}