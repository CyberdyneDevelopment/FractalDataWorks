using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// A pipeline type (FDW-6244): an implementation of the Pipeline domain, an option of <c>PipelineTypes</c>, found by the name a pipeline's domain row gives
/// as its <c>Implementation</c>.
/// </summary>
/// <remarks>
/// Every option implements this one non-generic interface whatever its generic arguments: the collection looks its options up through it, so an option
/// over its own closed base (as <c>GraphPipelineType</c> over <c>GraphPipelineTypeBase&lt;TIn, TOut&gt;</c>) is found like any other.
/// </remarks>
public interface IPipelineType : IServiceType
{
    /// <summary>Gets the name of the <c>PipelineRunModes</c> option a run of this type is carried out in.</summary>
    string RunModeName { get; }

    /// <summary>
    /// Gets the facts a pipeline of this type must state in its configuration (FDW-6141), none for a type that requires none.
    /// </summary>
    /// <remarks>
    /// The layer checks it twice: at Initialize the type's configuration must implement each interface (every gap in one pass), and at the save a row must state each fact.
    /// </remarks>
    IReadOnlyList<PipelineConfigurationRequirement> ConfigurationRequirements { get; }

    /// <summary>
    /// Gets whether a run of this type is for a person (FDW-6137): a start of it that acts as no person (a caller that is the system, a machine client or a run that acts as none) is refused.
    /// </summary>
    bool RequiresPerson { get; }
}

/// <summary>A pipeline type with its pipeline, configuration and factory types (FDW-6244).</summary>
/// <typeparam name="TService">The pipeline the type's factory makes.</typeparam>
/// <typeparam name="TConfiguration">The type's implementation row.</typeparam>
/// <typeparam name="TFactory">The factory that makes the pipeline from its row.</typeparam>
public interface IPipelineType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, IPipelineType
    where TService : IPipeline
    where TConfiguration : IGenericConfiguration
    where TFactory : IServiceFactory<TService, TConfiguration>
{
}
