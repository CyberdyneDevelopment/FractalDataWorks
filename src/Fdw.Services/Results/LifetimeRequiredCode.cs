using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Lifetime was null.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "LifetimeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LifetimeRequiredCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LifetimeRequiredCode"/> class.
    /// </summary>
    public LifetimeRequiredCode()
        : base(21001, "LifetimeRequired",
            ResultSeverities.ByName("Error"),
            "Lifetime cannot be null",
            isRetryable: false)
    {
    }
}