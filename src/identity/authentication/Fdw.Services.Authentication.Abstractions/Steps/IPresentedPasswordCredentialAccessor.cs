namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// The <see cref="IPasswordCredentialAccessor"/> a transport presents a credential through: it can also set
/// the credential for the current call flow.
/// </summary>
/// <remarks>
/// A transport sets it through a <see cref="PresentedPasswordCredentialScope"/> around the flow it runs; the
/// step that verifies the credential reads it through <see cref="IPasswordCredentialAccessor"/>.
/// </remarks>
public interface IPresentedPasswordCredentialAccessor : IPasswordCredentialAccessor
{
    /// <summary>Gets or sets the credential presented on the current call flow.</summary>
    PresentedPasswordCredential? Current { get; set; }
}
