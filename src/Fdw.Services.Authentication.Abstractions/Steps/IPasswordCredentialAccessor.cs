namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies the username and password the caller presented.
/// </summary>
/// <remarks>
/// A seam rather than parameters, because a step's signature is fixed by the pipeline. A host binds
/// this to wherever the credential actually arrives — a JSON body, a form field, a header — without
/// the step knowing which.
/// </remarks>
public interface IPasswordCredentialAccessor
{
    /// <summary>Gets the presented username, or null when none was.</summary>
    string? Username { get; }

    /// <summary>Gets the presented password, or null when none was.</summary>
    /// <remarks>
    /// Read once at the point of verification and never stored on the context: a credential that
    /// reaches the context is a credential that reaches every later step and anything that logs one.
    /// </remarks>
    string? Password { get; }
}
