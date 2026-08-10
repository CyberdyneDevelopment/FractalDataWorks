using System;
using System.Collections.Generic;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// DTO for connection authentication/security key-value properties.
/// The meaning of each value depends on the authentication type discriminator
/// (AuthenticationType on the request or AuthenticationType in the detail DTO).
/// </summary>
public class ConnectionAuthenticationDto
{
    /// <summary>Gets or sets the authentication property values (e.g., Username, SecretKeyName, ClientId).</summary>
    public IDictionary<string, string?> Values { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
