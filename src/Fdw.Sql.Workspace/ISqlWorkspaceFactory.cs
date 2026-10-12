using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Sql.Workspace;

/// <summary>Loads a .sqlproj into an <see cref="ISqlWorkspace"/>.</summary>
public interface ISqlWorkspaceFactory
{
    Task<IGenericResult<ISqlWorkspace>> Load(string sqlprojPath, CancellationToken cancellationToken = default);
}
