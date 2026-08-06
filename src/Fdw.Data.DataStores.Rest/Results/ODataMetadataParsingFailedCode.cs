using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Failed to parse OData metadata.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "ODataMetadataParsingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataMetadataParsingFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataMetadataParsingFailedCode"/> class.
    /// </summary>
    public ODataMetadataParsingFailedCode()
        : base(90003, "ODataMetadataParsingFailed",
            ResultSeverities.ByName("Error"),
            "Failed to parse OData metadata",
            isRetryable: false)
    {
    }
}