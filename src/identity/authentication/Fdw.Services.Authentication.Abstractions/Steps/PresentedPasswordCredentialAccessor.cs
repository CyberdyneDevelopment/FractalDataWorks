using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// The <see cref="IPasswordCredentialAccessor"/> every transport presents through: the credential the
/// current logical call flow was handed, held on an <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// A credential arrives over more than one transport — an HTTP login body, a SQL Server login packet —
/// and the step that verifies it must not know which. Each transport presents what it received through
/// a <see cref="PresentedPasswordCredentialScope"/> around the flow it runs, and the step reads it here.
/// </para>
/// <para>
/// Safe as a DI singleton for the same reason <see cref="Security.AuthenticationContextAccessor"/> is:
/// the value flows only through the awaited continuations of the call flow that set it, so two logins
/// running at once never see each other's credential. Outside a scope nothing was presented, and both
/// properties answer null.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PresentedPasswordCredentialAccessor : IPasswordCredentialAccessor
{
    // The instance's own slot: the scope a transport opens and the step that verifies read one accessor, the
    // singleton DI hands both, so they meet in the same slot.
    private readonly AsyncLocal<PresentedPasswordCredential?> _current = new();

    /// <summary>Gets or sets the credential presented on the current call flow.</summary>
    /// <remarks>Set through <see cref="PresentedPasswordCredentialScope"/>, which restores it on exit.</remarks>
    public PresentedPasswordCredential? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <inheritdoc/>
    public string? Username => _current.Value?.Username;

    /// <inheritdoc/>
    public string? Password => _current.Value?.Password;
}
