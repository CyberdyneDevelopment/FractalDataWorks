using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Configuration entry has no Implementation — factory resolution cannot proceed.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "ImplementationMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ImplementationMissingCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplementationMissingCode"/> class.
    /// </summary>
    public ImplementationMissingCode()
        : base(60000, "ImplementationMissing",
            ResultSeverities.ByName("Error"),
            "Configuration '{Identifier}' has no Implementation — cannot resolve factory",
            isRetryable: false)
    {
    }
}
