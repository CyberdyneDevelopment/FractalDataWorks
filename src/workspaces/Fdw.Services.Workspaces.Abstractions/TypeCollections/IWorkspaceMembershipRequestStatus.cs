using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Where a membership request stands in its review.</summary>
public interface IWorkspaceMembershipRequestStatus : ITypeOption<int, WorkspaceMembershipRequestStatusBase>
{
}
