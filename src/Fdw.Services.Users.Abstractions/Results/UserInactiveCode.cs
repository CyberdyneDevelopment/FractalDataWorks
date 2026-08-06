using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// User is inactive.
/// </summary>
[TypeOption(typeof(UserResultCodes), "UserInactive", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserInactiveCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserInactiveCode"/> class.
    /// </summary>
    public UserInactiveCode()
        : base(40000, "UserInactive",
            ResultSeverities.ByName("Error"),
            "User account is inactive",
            isRetryable: false)
    {
    }
}