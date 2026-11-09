using Fdw.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>Base of the workspace service types.</summary>
public abstract class WorkspaceServiceTypeBase : ServiceTypeBase<IGenericService, IWorkspaceServiceFactory, IServiceConfiguration>, IWorkspaceServiceType
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceServiceTypeBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="sectionName">The configuration section.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">What it registers.</param>
    protected WorkspaceServiceTypeBase(string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description, "Workspaces")
    {
    }
}
