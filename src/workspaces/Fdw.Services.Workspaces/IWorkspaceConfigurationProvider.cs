using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Reads and writes workspace configurations.
/// </summary>
/// <remarks>
/// Three Get overloads and nothing else: by name, by id, and all of them. A caller that wants
/// one workspace's members reads the workspace and walks it, rather than asking a provider for a
/// filtered slice — the aggregate the provider returns is already navigable.
/// </remarks>
public interface IWorkspaceConfigurationProvider

    : IDomainConfigurationProvider<IWorkspaceImplementationConfiguration>
{

}
