namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// A secret a connection's credential is stored in: the secret manager and the secret's name. Never its value.
/// </summary>
/// <param name="SecretManagerName">The secret manager the secret is in.</param>
/// <param name="SecretKey">The secret's name in that manager.</param>
public sealed record StoredSecret(string SecretManagerName, string SecretKey);
