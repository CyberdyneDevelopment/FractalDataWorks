using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels;

/// <summary>
/// Base class for language model provider type definitions: the option a provider package declares to join <see cref="LanguageModelTypes"/>.
/// </summary>
/// <typeparam name="TService">The language model type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating language models.</typeparam>
/// <typeparam name="TConfiguration">The provider's configuration type.</typeparam>
/// <remarks>Provider types supply type metadata in their constructors. Instantiation logic belongs in factories.</remarks>
public abstract class LanguageModelTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    ILanguageModelType<TService, TFactory, TConfiguration>
    where TService : ILanguageModel
    where TFactory : ILanguageModelFactory<TService, TConfiguration>
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelTypeBase{TService, TFactory, TConfiguration}"/> class.</summary>
    /// <param name="name">The option name, for example the provider's own name.</param>
    /// <param name="sectionName">The configuration section name for IOptions binding.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="description">What this provider offers.</param>
    /// <param name="defaultContainerName">This option's implementation configuration table: {Implementation}LanguageModelConfiguration. Required, so every option states it.</param>
    protected LanguageModelTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string defaultContainerName)
        : base(name, sectionName, displayName, description, "LanguageModel",
               defaultDataStoreName: () => PlatformConnection.Name,
               defaultPathName: "agent",
               defaultContainerName: defaultContainerName)
    {
    }
}
