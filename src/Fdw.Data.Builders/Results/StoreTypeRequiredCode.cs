using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Store type is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "StoreTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreTypeRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreTypeRequiredCode"/> class.
    /// </summary>
    public StoreTypeRequiredCode()
        : base(21001, "StoreTypeRequired",
            ResultSeverities.ByName("Error"),
            "Store type is required",
            isRetryable: false)
    {
    }
}