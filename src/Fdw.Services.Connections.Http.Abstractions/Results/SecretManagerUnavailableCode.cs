using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// The security configuration names secrets to resolve but no secret manager was supplied.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SecretManagerUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretManagerUnavailableCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes the code. Category 6 (Configuration): the connection is misconfigured for
    /// this call path — secrets are demanded but no ISecretManager is available to resolve them.
    /// </summary>
    public SecretManagerUnavailableCode()
        : base(60001, "SecretManagerUnavailable",
            ResultSeverities.ByName("Error"),
            "Security configuration names secrets but no secret manager was supplied",
            isRetryable: false)
    {
    }
}
