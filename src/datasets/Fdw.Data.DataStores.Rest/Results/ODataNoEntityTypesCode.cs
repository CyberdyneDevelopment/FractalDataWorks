using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// The OData $metadata document declares no EntityType.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "ODataNoEntityTypes", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataNoEntityTypesCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataNoEntityTypesCode"/> class.
    /// </summary>
    public ODataNoEntityTypesCode()
        : base(20002, "ODataNoEntityTypes",
            ResultSeverities.ByName("Error"),
            "The $metadata document declares no EntityType in the OData v4 or v3 namespace, so there is no schema to import",
            isRetryable: false)
    {
    }
}