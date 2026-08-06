using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// No parent configuration provider registered — service lookup cannot proceed.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "NoParentProvider", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoParentProviderCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoParentProviderCode"/> class.
    /// </summary>
    public NoParentProviderCode()
        : base(61003, "NoParentProvider",
            ResultSeverities.ByName("Error"),
            "No parent configuration provider registered — cannot resolve '{Identifier}'",
            isRetryable: false)
    {
    }
}
