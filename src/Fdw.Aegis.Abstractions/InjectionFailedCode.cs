using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// <c>Aegis.Injector</c> resolved the secret but the downstream injection call failed.
/// </summary>
[TypeOption(typeof(AegisResultCodes), "InjectionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InjectionFailedCode : AegisResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionFailedCode"/> class.
    /// </summary>
    public InjectionFailedCode()
        : base(71001, "InjectionFailed",
            ResultSeverities.ByName("Error"),
            "Injection failed for command '{commandName}': {reason}",
            isRetryable: true)
    {
    }
}
