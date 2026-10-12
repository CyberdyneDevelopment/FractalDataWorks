using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// OData import operation failed.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "ODataImportFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataImportFailedCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataImportFailedCode"/> class.
    /// </summary>
    public ODataImportFailedCode()
        : base(70003, "ODataImportFailed",
            ResultSeverities.ByName("Error"),
            "OData schema import failed",
            isRetryable: true)
    {
    }
}