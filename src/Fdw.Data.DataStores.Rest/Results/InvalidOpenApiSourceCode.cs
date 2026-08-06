using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Invalid OpenAPI source.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "InvalidOpenApiSource", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidOpenApiSourceCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidOpenApiSourceCode"/> class.
    /// </summary>
    public InvalidOpenApiSourceCode()
        : base(21000, "InvalidOpenApiSource",
            ResultSeverities.ByName("Error"),
            "Invalid OpenAPI source: {ErrorMessage}",
            isRetryable: false)
    {
    }
}