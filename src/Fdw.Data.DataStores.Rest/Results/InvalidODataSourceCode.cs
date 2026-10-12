using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Invalid OData source.
/// </summary>
[TypeOption(typeof(RestDataStoreResultCodes), "InvalidODataSource", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidODataSourceCode : RestDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidODataSourceCode"/> class.
    /// </summary>
    public InvalidODataSourceCode()
        : base(20001, "InvalidODataSource",
            ResultSeverities.ByName("Error"),
            "Invalid OData source: {ErrorMessage}",
            isRetryable: false)
    {
    }
}