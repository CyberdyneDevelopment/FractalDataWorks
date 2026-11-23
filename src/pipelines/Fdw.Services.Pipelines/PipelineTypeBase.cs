using Fdw.Services.Descriptions;
using System.Collections.Generic;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Logging;
using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines;

/// <summary>
/// The base of the options of <see cref="PipelineTypes"/> (FDW-6244): an implementation of the Pipeline domain, with the pipeline its factory makes, its
/// implementation row and the run mode its runs are carried out in.
/// </summary>
/// <typeparam name="TService">The pipeline the type's factory makes.</typeparam>
/// <typeparam name="TFactory">The factory that makes the pipeline from its row.</typeparam>
/// <typeparam name="TConfiguration">The type's implementation row.</typeparam>
/// <remarks>
/// An option need not close this base with the collection's own arguments: an implementation closes it over its own pipeline and row (as
/// <c>GraphPipelineTypeBase&lt;TIn, TOut&gt;</c> does), and the collection finds it through <see cref="IPipelineType"/>. A base that sits between this one and
/// its options sets its phase bodies once, in its own constructor; an option that called <c>Registration</c> again would replace the base's body.
/// </remarks>
public abstract class PipelineTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IPipelineType<TService, TConfiguration, TFactory>
    where TService : IPipeline
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IServiceFactory<TService, TConfiguration>
{
    /// <inheritdoc />
    /// <remarks>A type requires nothing of its configuration unless it says so (FDW-6141).</remarks>
    public virtual IReadOnlyList<PipelineConfigurationRequirement> ConfigurationRequirements => [];

    /// <inheritdoc />
    /// <remarks>A run of a type is for no person in particular unless the type says so (FDW-6137).</remarks>
    public virtual bool RequiresPerson => false;

    /// <summary>Gets the facts a pipeline of this type must state, by name: what the type-collection route reports to a client that lets a person choose the type (FDW-6738).</summary>
    /// <remarks>
    /// Why this name and not a second list: <c>RequiredProperties</c> is the one answer to "what must I state when I choose this option" the type-collection summary already
    /// reads from an option by name (the authentication and paging options state it so). It is computed from <see cref="ConfigurationRequirements"/>, so the two cannot differ.
    /// </remarks>
    public IReadOnlyList<string> RequiredProperties => [.. ConfigurationRequirements.Select(requirement => requirement.FactName)];

    /// <summary>The category a type option states none of its own sits under.</summary>
    [PlatformDefault]
    private const string DefaultCategory = "Pipeline";

    /// <summary>Initializes a new instance of the <see cref="PipelineTypeBase{TService,TFactory,TConfiguration}"/> class.</summary>
    /// <param name="name">The implementation's name, as a pipeline's domain row gives it.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name of this pipeline type.</param>
    /// <param name="description">What a pipeline of this type is.</param>
    /// <param name="runModeName">The name of the <c>PipelineRunModes</c> option a run of this type is carried out in; one no option names refuses every start.</param>
    /// <param name="category">The category of this pipeline type.</param>
    /// <param name="defaultContainerName">The container of this type's implementation rows.</param>
    protected PipelineTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string runModeName,
        string? category = null,
        string defaultContainerName = "")
        : base(name, sectionName, displayName, description, category ?? DefaultCategory,
               defaultDataStoreName: () => PlatformConnection.Name,
               defaultPathName: "pipe",
               defaultContainerName: defaultContainerName)
    {
        RunModeName = runModeName;

        // Why required of every option: the domain row every implementation row hangs from is read through the pipe.Pipeline configuration provider,
        // which PipelineTypes registers; a host that replaced the domain's Registration without it is refused at Initialize, naming it.
        Requires<IPipelineConfigurationProvider>(PipelineTypesLog.ConfigurationProviderNotRegistered);

        // Why required of every option (FDW-7323): the pipeline provider writes each pipeline's and parameter's description in the unit of the save, which needs the descriptions writer.
        Requires<IDescriptionWriter>(PipelineTypesLog.DescriptionWriterNotRegistered);
    }

    /// <inheritdoc />
    public string RunModeName { get; }
}
