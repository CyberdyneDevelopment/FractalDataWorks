using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// User already exists.
/// </summary>
[TypeOption(typeof(UserResultCodes), "UserAlreadyExists", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserAlreadyExistsCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserAlreadyExistsCode"/> class.
    /// </summary>
    public UserAlreadyExistsCode()
        : base(40001, "UserAlreadyExists",
            ResultSeverities.ByName("Error"),
            "User '{username}' already exists",
            isRetryable: false)
    {
    }
}