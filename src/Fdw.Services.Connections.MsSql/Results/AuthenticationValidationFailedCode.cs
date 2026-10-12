using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Authentication validation failed.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "AuthenticationValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AuthenticationValidationFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationValidationFailedCode"/> class.
    /// </summary>
    public AuthenticationValidationFailedCode()
        : base(
            51000,
            "AuthenticationValidationFailed",
            ResultSeverities.ByName("Error"),
            "Authentication validation failed: {ValidationErrors}",
            isRetryable: false)
    {
    }
}
