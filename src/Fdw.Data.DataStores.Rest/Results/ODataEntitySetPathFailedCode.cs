using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Failed to create OData EntitySet path.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "ODataEntitySetPathFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataEntitySetPathFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataEntitySetPathFailedCode"/> class.
    /// </summary>
    public ODataEntitySetPathFailedCode()
        : base(90001, "ODataEntitySetPathFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create EntitySet path for '{EntitySetName}': {ErrorMessage}",
            isRetryable: false)
    {
    }
}