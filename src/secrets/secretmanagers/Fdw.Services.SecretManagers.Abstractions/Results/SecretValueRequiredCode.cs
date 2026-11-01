using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Secret value parameter is required for the operation.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "SecretValueRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretValueRequiredCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretValueRequiredCode"/> class.
    /// </summary>
    public SecretValueRequiredCode()
        : base(21002, "SecretValueRequired",
            ResultSeverities.ByName("Error"),
            "SecretValue parameter is required for {Operation} operation",
            isRetryable: false)
    {
    }
}
