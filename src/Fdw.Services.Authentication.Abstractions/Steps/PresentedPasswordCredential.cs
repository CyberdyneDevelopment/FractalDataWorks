namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A username and password exactly as a caller presented them, before anything has verified them.
/// </summary>
/// <remarks>
/// Either value may be absent: a caller can send a login with a field missing, and the step that
/// verifies the credential is the one that refuses it. Nothing here supplies a value the caller did not.
/// </remarks>
public sealed class PresentedPasswordCredential
{
    /// <summary>Initializes a new instance of the <see cref="PresentedPasswordCredential"/> class.</summary>
    /// <param name="username">The username the caller presented, or null when none was.</param>
    /// <param name="password">The password the caller presented, or null when none was.</param>
    public PresentedPasswordCredential(string? username, string? password)
    {
        Username = username;
        Password = password;
    }

    /// <summary>Gets the presented username, or null when none was.</summary>
    public string? Username { get; }

    /// <summary>Gets the presented password, or null when none was.</summary>
    public string? Password { get; }

    /// <summary>Names the credential by its username only.</summary>
    /// <returns>The username, so a credential that reaches a log never carries its password.</returns>
    public override string ToString() => $"{Username}";
}
