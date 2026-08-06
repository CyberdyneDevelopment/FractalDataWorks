using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Default <see cref="IAuthenticationContextAccessor"/> — backed by
/// <see cref="AsyncLocal{T}"/> so it is safe to register and consume as a DI <c>Singleton</c> (see
/// <see cref="IAuthenticationContextAccessor"/> remarks for why that matters).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthenticationContextAccessor : IAuthenticationContextAccessor
{
    // Why static: AsyncLocal<T> already scopes by logical call flow (the .NET ExecutionContext), not by
    // instance — a static field here mirrors Microsoft's own HttpContextAccessor implementation exactly.
    // An instance field would work identically since this class itself is registered Singleton, but static
    // makes the "one ambient slot per flow, not per accessor instance" invariant explicit.
    private static readonly AsyncLocal<IAuthenticationContext?> _current = new();

    /// <inheritdoc/>
    public IAuthenticationContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
