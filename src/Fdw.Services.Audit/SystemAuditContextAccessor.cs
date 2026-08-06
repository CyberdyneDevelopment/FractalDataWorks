using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Audit.Abstractions;

namespace Fdw.Services.Audit;

/// <summary>
/// Default <see cref="IAuditContextAccessor"/> for environments where no caller
/// information is available — used by background jobs and as a safety fallback.
/// Always returns the same "system" context.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SystemAuditContextAccessor : IAuditContextAccessor
{
    private static readonly AuditContext _systemContext = new()
    {
        UserId = "system",
        UserName = "system",
    };

    /// <inheritdoc />
    public AuditContext GetContext() => _systemContext;
}
