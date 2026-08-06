using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Workspace;

/// <summary>Default <see cref="ISqlWorkspaceFactory"/> backed by <see cref="SqlWorkspace.Load"/>.</summary>
public sealed class SqlWorkspaceFactory : ISqlWorkspaceFactory
{
    private readonly ILogger<SqlWorkspace> _logger;

    public SqlWorkspaceFactory(ILogger<SqlWorkspace>? logger = null)
    {
        _logger = logger ?? NullLogger<SqlWorkspace>.Instance;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISqlWorkspace>> Load(string sqlprojPath, CancellationToken cancellationToken = default)
        => SqlWorkspace.Load(sqlprojPath, _logger, cancellationToken);
}
