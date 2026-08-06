using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// Invalid credentials.
/// </summary>
[TypeOption(typeof(UserResultCodes), "InvalidCredentials", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidCredentialsCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsCode"/> class.
    /// </summary>
    public InvalidCredentialsCode()
        : base(51000, "InvalidCredentials",
            ResultSeverities.ByName("Error"),
            "Invalid credentials",
            isRetryable: false)
    {
    }
}