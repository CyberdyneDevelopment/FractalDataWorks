using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The account was created and a later step was not. The account row lives in ConfigurationDb and
/// its credentials live in AuthDb, so the two cannot be written in one transaction; this code says
/// which step failed and what already exists, so the caller finishes the job instead of repeating it.
/// </summary>
[TypeOption(typeof(UserResultCodes), "UserPartiallyCreated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserPartiallyCreatedCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPartiallyCreatedCode"/> class.
    /// </summary>
    public UserPartiallyCreatedCode()
        : base(40004, "UserPartiallyCreated",
            ResultSeverities.ByName("Error"),
            "The account was created, but one of its sign-in methods was not.",
            isRetryable: false)
    {
    }
}
