using System;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Presents a credential to the authentication flow run inside it, and takes it back on
/// <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// A transport opens one around exactly the flow it runs for the caller, so the credential is readable
/// only while that flow verifies it. The value held before entry is restored rather than cleared, so a
/// scope opened inside another never leaves the outer one's credential replaced.
/// <code>
/// using (new PresentedPasswordCredentialScope(presented, username, password))
/// {
///     var result = await runner.Run(flow, cancellationToken);
/// }
/// </code>
/// </remarks>
public sealed class PresentedPasswordCredentialScope : IDisposable
{
    private readonly PresentedPasswordCredentialAccessor _accessor;
    private readonly PresentedPasswordCredential? _previous;
    private bool _disposed;

    /// <summary>
    /// Enters the scope: saves <paramref name="accessor"/>'s current credential and presents this one.
    /// </summary>
    /// <param name="accessor">The ambient accessor the verifying step reads.</param>
    /// <param name="username">The username the caller presented, or null when none was.</param>
    /// <param name="password">The password the caller presented, or null when none was.</param>
    public PresentedPasswordCredentialScope(PresentedPasswordCredentialAccessor accessor, string? username, string? password)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _previous = accessor.Current;
        accessor.Current = new PresentedPasswordCredential(username, password);
    }

    /// <summary>Takes the credential back, restoring whatever was presented before this scope.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _accessor.Current = _previous;
        _disposed = true;
    }
}
