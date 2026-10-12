using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL login failed (error 18456). The database credentials are incorrect or the login does not exist.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "LoginFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LoginFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginFailedCode"/> class.
    /// </summary>
    public LoginFailedCode()
        : base(50002, "LoginFailed", ResultSeverities.ByName("Error"),
            "Login failed for the database user. Check that the login exists and the password matches the FDW_SECRET_* environment variable.",
            isRetryable: false)
    {
    }
}
