using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// ServiceTypeCollection for the workspace domain: the providers, the workspace permissions and the maker of a default workspace (FDW-4556).
/// </summary>
/// <remarks>
/// Registered by the package itself, so any host that references Fdw.Services.Workspaces can found a tenant with its default workspace whether or not it serves the
/// workspace endpoints, which need the tag, pipeline, category and data set providers a founding host may not have.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(WorkspaceServiceTypeBase),
    typeof(IWorkspaceServiceType),
    typeof(WorkspaceServiceTypes),
    ServiceCategory = "Workspaces",
    RestrictToCurrentCompilation = true)]
public partial class WorkspaceServiceTypes : ServiceTypeCollectionBase<WorkspaceServiceTypeBase, IWorkspaceServiceType>
{
    /// <summary>The connection this domain's configuration rows are read from and written to.</summary>
    public static string ConfigurationConnection => PlatformConnection.Name;
}
