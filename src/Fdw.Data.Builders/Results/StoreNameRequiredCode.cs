using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Store name is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "StoreNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreNameRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreNameRequiredCode"/> class.
    /// </summary>
    public StoreNameRequiredCode()
        : base(21000, "StoreNameRequired",
            ResultSeverities.ByName("Error"),
            "Store name is required",
            isRetryable: false)
    {
    }
}