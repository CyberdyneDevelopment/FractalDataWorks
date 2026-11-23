using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>Marker interface for language model factories.</summary>
public interface ILanguageModelFactory
{
}

/// <summary>Generic interface for language model factories with typed configuration.</summary>
/// <typeparam name="TService">The language model type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the model.</typeparam>
public interface ILanguageModelFactory<TService, TConfiguration> : ILanguageModelFactory, IServiceFactory<TService, TConfiguration>
    where TService : ILanguageModel
    where TConfiguration : IGenericConfiguration
{
}
