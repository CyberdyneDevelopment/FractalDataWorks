using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Says whether founding a tenant or an organization makes a default workspace (FDW-4599).
/// </summary>
public interface IWorkspaceFoundingPolicy
{
    /// <summary>Reads the setting <see cref="WorkspaceFoundingOption.FoundingMakesDefaultWorkspace"/>.</summary>
    /// <returns>The value; a coded failure when no settings row, host or option states it.</returns>
    IGenericResult<bool> FoundingMakesDefaultWorkspace();
}
