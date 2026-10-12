using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Secret key is required for the operation.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "SecretKeyRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretKeyRequiredCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretKeyRequiredCode"/> class.
    /// </summary>
    public SecretKeyRequiredCode()
        : base(20000, "SecretKeyRequired",
            ResultSeverities.ByName("Error"),
            "Secret key is required for {Operation} operation",
            isRetryable: false)
    {
    }
}
