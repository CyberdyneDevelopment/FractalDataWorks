using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Store ID is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "StoreIdRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreIdRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreIdRequiredCode"/> class.
    /// </summary>
    public StoreIdRequiredCode()
        : base(20000, "StoreIdRequired",
            ResultSeverities.ByName("Error"),
            "Store ID is required",
            isRetryable: false)
    {
    }
}