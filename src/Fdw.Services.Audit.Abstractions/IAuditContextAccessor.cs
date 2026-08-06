namespace Fdw.Services.Audit.Abstractions;

/// <summary>
/// Resolves the <see cref="AuditContext"/> for the current call site.
/// Each surface (HTTP endpoint, CLI, background job) registers its own implementation
/// so cross-cutting components like <c>AuditingConfigurationWriter&lt;T&gt;</c> can
/// fetch the active caller without knowing the transport layer.
/// </summary>
// Why: Endpoints currently build AuditContext inline from HttpContext. The decorator
// pattern needs an injectable accessor so the same component works in CLI, jobs, and
// other non-HTTP contexts. Web hosting will register an HttpContext-backed accessor;
// CLI will register a profile-backed accessor; background jobs a "system" accessor.
public interface IAuditContextAccessor
{
    /// <summary>
    /// Returns the audit context for the active call. Implementations should never
    /// throw — fall back to a deterministic "system" context when no caller is in scope.
    /// </summary>
    AuditContext GetContext();
}
