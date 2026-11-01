using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// <c>Aegis.Injector</c> failed to resolve the referenced secret from its declared secret manager.
/// </summary>
[TypeOption(typeof(AegisResultCodes), "SecretResolutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretResolutionFailedCode : AegisResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretResolutionFailedCode"/> class.
    /// </summary>
    public SecretResolutionFailedCode()
        : base(71000, "SecretResolutionFailed",
            ResultSeverities.ByName("Error"),
            "Failed to resolve secret '{secretKeyName}' from manager '{secretManagerName}'.",
            isRetryable: true)
    {
    }
}
