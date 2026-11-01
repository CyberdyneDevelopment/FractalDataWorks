using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Failed to fetch OData metadata from endpoint.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "ODataMetadataFetchFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataMetadataFetchFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataMetadataFetchFailedCode"/> class.
    /// </summary>
    public ODataMetadataFetchFailedCode()
        : base(70000, "ODataMetadataFetchFailed",
            ResultSeverities.ByName("Error"),
            "Failed to fetch OData metadata: {ErrorMessage}",
            isRetryable: true)
    {
    }
}