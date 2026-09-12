namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A secret an authentication type needs fetched before it can build its fragment: which manager
/// holds it, and under which key.
/// </summary>
/// <param name="ManagerName">The secret manager the connection declares.</param>
/// <param name="KeyName">The key to read out of that manager.</param>
/// <remarks>
/// This is how the evaluation stays with the authentication type while the fetching stays with the
/// provider. The type decides whether it needs a secret at all, which manager holds it and under
/// which key — it owns <c>SecretManagerName</c> and <c>SecretKeyName</c>, they are its properties.
/// What it does NOT do is go and get it: resolving a manager and executing a read are the provider's
/// work, because the provider is what holds <c>ISecretManagerProvider</c>. The type then builds the
/// fragment from the value it is handed.
///
/// Naming the manager here rather than letting the provider choose one is what makes a silent
/// credential substitution impossible: the provider reads the store the connection declared, or it
/// fails — it is never in a position to prefer a different one.
/// </remarks>
public readonly record struct SecretRequirement(string ManagerName, string KeyName)
{
    /// <summary>An authentication type that needs no secret — the default.</summary>
    public static SecretRequirement None => default;

    /// <summary>Whether this requirement names nothing to fetch.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(KeyName);
}
