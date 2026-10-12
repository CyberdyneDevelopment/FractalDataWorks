using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Failed to list secrets.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "ListSecretsFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ListSecretsFailedCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSecretsFailedCode"/> class.
    /// </summary>
    public ListSecretsFailedCode()
        : base(70000, "ListSecretsFailed",
            ResultSeverities.ByName("Error"),
            "Failed to list secrets: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
