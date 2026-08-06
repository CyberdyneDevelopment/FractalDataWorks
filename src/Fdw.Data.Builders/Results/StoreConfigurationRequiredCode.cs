using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Store configuration is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "StoreConfigurationRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreConfigurationRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreConfigurationRequiredCode"/> class.
    /// </summary>
    public StoreConfigurationRequiredCode()
        : base(21004, "StoreConfigurationRequired",
            ResultSeverities.ByName("Error"),
            "Store configuration is required",
            isRetryable: false)
    {
    }
}