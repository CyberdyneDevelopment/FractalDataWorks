using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The permission named is not one this platform defines.
/// </summary>
/// <remarks>
/// A gate naming a permission nobody can hold would lock every user out of the flow, so it is refused before it is saved rather than discovered at somebody's sign-in. An endpoint answers it with a 400.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "PermissionNotDefined", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PermissionNotDefinedCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PermissionNotDefinedCode"/> class.</summary>
    public PermissionNotDefinedCode()
        : base(20001, "PermissionNotDefined",
            ResultSeverities.ByName("Error"),
            "No permission of that name is defined",
            isRetryable: false)
    {
    }
}
