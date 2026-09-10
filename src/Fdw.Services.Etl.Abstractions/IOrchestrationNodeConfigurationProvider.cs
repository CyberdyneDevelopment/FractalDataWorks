using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Configuration provider for OrchestrationNode — the self-referencing-tree (node→parent, same table) domain.
/// The tree overloads (Get(name,parentId)/Get(id,depth)/GetRoots/GetChildren) are the sanctioned carve-out
/// over the keystone base, which loads the flat rows; the plain Get(id)/Get()/Save/Delete are the base's.
/// All typed ergonomic providers (IProjectConfigurationProvider, IStageConfigurationProvider,
/// IStepConfigurationProvider) are thin wrappers over this interface.
/// </summary>
public interface IOrchestrationNodeConfigurationProvider

    : IDomainConfigurationProvider<IOrchestrationNodeImplementationConfiguration>
{

}
