using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>The factory contract of the workspace service type: it makes no service of its own; the type only registers the workspace providers (FDW-4556).</summary>
public interface IWorkspaceServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
