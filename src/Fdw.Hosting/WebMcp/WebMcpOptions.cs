using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.WebMcp;

/// <summary>
/// Configuration options for WebMCP tool exposure and agent key authentication.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class WebMcpOptions
{
    /// <summary>
    /// API key injected into the generated webmcp.js fetch calls.
    /// This value is embedded in the public JavaScript file — use a low-privilege,
    /// read-only key only. Store the value in secrets, not appsettings.json.
    /// </summary>
    public string? ClientApiKey { get; set; }

    /// <summary>
    /// HTTP header name used to carry the API key. Default: <c>X-Webmcp-Key</c>.
    /// Must match on both client (generated JS) and server (middleware).
    /// </summary>
    public string ApiKeyHeader { get; set; } = "X-Webmcp-Key";

    /// <summary>
    /// Agent API keys accepted by the server. Each key is associated with a user identity
    /// so that RBAC applies and audit logs show who authorized the agent.
    /// </summary>
    public IList<WebMcpAgentKey> AgentKeys { get; set; } = [];
}
