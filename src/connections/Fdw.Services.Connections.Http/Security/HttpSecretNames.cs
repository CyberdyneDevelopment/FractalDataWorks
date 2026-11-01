namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// The names of the secrets an authentication method reads, by the slot of the HTTP protocol context each travels in (FDW-2844).
/// </summary>
/// <param name="CertificateSecretName">The secret holding the client certificate; null when the method reads none.</param>
/// <param name="PasswordSecretName">The secret holding the password; null when the method reads none.</param>
/// <param name="ApiKeySecretName">The secret holding the API key or bearer token; null when the method reads none.</param>
public readonly record struct HttpSecretNames(string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName)
{
    /// <summary>Gets the names of a method that reads no secret.</summary>
    public static HttpSecretNames None => default;
}
