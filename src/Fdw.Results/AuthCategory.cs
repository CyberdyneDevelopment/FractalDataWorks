using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 5 (50000–59999): authentication or authorization failure.
/// </summary>
[TypeOption(typeof(ResultCategories), "Auth", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AuthCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthCategory"/> class.
    /// </summary>
    public AuthCategory()
        : base(id: 5, name: "Auth", isFailure: true, isRetryable: false, httpStatus: 401, clientMessage: "Authentication failed or is required", clientAction: "Sign in and try again")
    {
    }
}
