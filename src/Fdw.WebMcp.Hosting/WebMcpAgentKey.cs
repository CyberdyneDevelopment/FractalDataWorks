using System.Diagnostics.CodeAnalysis;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Represents an API key that authorizes an AI agent to act on behalf of a user.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class WebMcpAgentKey
{
    /// <summary>
    /// The raw API key value. Store in secrets (environment variables or secret manager),
    /// not in appsettings.json.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The user ID this key acts on behalf of. Should match the user's ID in usr.Users
    /// so that the normal FDW RBAC system can evaluate the user's permissions.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for audit logs and diagnostics.
    /// Convention: "{person} - {agent description}", e.g. "mike - claude dev agent".
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
