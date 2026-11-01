namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// The sign-in methods to create with a new user. At least one is required: an account with no
/// method has no way in.
/// </summary>
public class SignInMethodsRequest
{
    /// <summary>
    /// Gets or sets the password method, when the account signs in with a password.
    /// </summary>
    public PasswordSignInMethod? Password { get; set; }

    /// <summary>
    /// Gets or sets the external-identity method, when the account signs in through a provider.
    /// The provider is named in the method itself, so this carries no vendor in its name.
    /// </summary>
    public ExternalSignInMethod? External { get; set; }

    /// <summary>
    /// Gets or sets the API key to mint with the account.
    /// </summary>
    public ApiKeySignInMethod? ApiKey { get; set; }
}
