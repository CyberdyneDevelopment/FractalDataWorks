using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// User not found.
/// </summary>
[TypeOption(typeof(UserResultCodes), "UserNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserNotFoundCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundCode"/> class.
    /// </summary>
    public UserNotFoundCode()
        : base(30000, "UserNotFound",
            ResultSeverities.ByName("Error"),
            "User not found",
            isRetryable: false)
    {
    }
}