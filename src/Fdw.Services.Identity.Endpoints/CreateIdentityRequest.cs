using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// The header fields common to creating any managed identity, whatever mechanism backs it.
/// </summary>
/// <remarks>
/// There is deliberately no field for a credential. A mechanism names where its secret is resolved
/// from — <c>SecretManagerName</c> and <c>SecretKeyName</c> — and the value itself is placed in that
/// secret manager out of band. A create call that carried the secret would put it in a request body,
/// an access log and a client's history, which is the thing the identity domain exists to avoid.
/// </remarks>
[ExcludeFromCodeCoverage]
public class CreateIdentityRequest
{
    /// <summary>Gets or sets the name the identity is addressed by.</summary>
    /// <remarks>This is what a caller names in its outbound-identity configuration.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the mechanism backing the identity.</summary>
    /// <remarks>
    /// A member of <c>IdentityServiceTypes</c> — the route already implies it, and the two must
    /// agree.
    /// </remarks>
    public string ServiceOptionType { get; set; } = string.Empty;

    /// <summary>Gets or sets a description of what authenticates as this identity.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the name of the secret manager the credential is resolved from.</summary>
    public string SecretManagerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the key the credential is stored under in that secret manager.</summary>
    public string SecretKeyName { get; set; } = string.Empty;
}
