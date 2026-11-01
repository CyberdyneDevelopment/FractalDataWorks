using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The change would leave the administrator making it with no way to sign in the way they signed in now.
/// </summary>
/// <remarks>
/// Refused so a sign-in change can always be undone by whoever made it. The administrator can make the change after first granting themselves the permission, or from a session signed in some other way. An endpoint answers it with a 409.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "ChangeLocksOutAdministrator", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ChangeLocksOutAdministratorCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ChangeLocksOutAdministratorCode"/> class.</summary>
    public ChangeLocksOutAdministratorCode()
        : base(41003, "ChangeLocksOutAdministrator",
            ResultSeverities.ByName("Error"),
            "The change would leave you no way to sign in the way you signed in now",
            isRetryable: false)
    {
    }
}
