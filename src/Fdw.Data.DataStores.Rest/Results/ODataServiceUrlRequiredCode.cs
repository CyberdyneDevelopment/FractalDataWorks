using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// OData service URL was null or empty.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "ODataServiceUrlRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataServiceUrlRequiredCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataServiceUrlRequiredCode"/> class.
    /// </summary>
    public ODataServiceUrlRequiredCode()
        : base(20000, "ODataServiceUrlRequired",
            ResultSeverities.ByName("Error"),
            "OData service URL cannot be null or empty",
            isRetryable: false)
    {
    }
}