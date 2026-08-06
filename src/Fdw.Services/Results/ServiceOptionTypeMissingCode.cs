using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Configuration entry has no ServiceOptionType — factory resolution cannot proceed.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "ServiceOptionTypeMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServiceOptionTypeMissingCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceOptionTypeMissingCode"/> class.
    /// </summary>
    public ServiceOptionTypeMissingCode()
        : base(60000, "ServiceOptionTypeMissing",
            ResultSeverities.ByName("Error"),
            "Configuration '{Identifier}' has no ServiceOptionType — cannot resolve factory",
            isRetryable: false)
    {
    }
}
