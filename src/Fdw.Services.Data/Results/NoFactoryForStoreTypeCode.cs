using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// No factory registered for the specified store type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "NoFactoryForStoreType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoFactoryForStoreTypeCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoFactoryForStoreTypeCode"/> class.
    /// </summary>
    public NoFactoryForStoreTypeCode()
        : base(61000, "NoFactoryForStoreType", ResultSeverities.ByName("Error"),
            "No factory registered for store type '{StoreType}'",
            isRetryable: false)
    {
    }
}