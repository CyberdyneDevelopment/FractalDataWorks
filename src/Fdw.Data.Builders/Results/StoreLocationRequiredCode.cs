using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Store location is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "StoreLocationRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreLocationRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreLocationRequiredCode"/> class.
    /// </summary>
    public StoreLocationRequiredCode()
        : base(21003, "StoreLocationRequired",
            ResultSeverities.ByName("Error"),
            "Store location is required",
            isRetryable: false)
    {
    }
}