using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>Marker interface for language model provider type definitions.</summary>
public interface ILanguageModelType : IServiceType
{
}

/// <summary>Generic interface for language model provider type definitions with typed parameters.</summary>
/// <typeparam name="TService">The language model type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating language models.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the model.</typeparam>
public interface ILanguageModelType<TService, TFactory, TConfiguration>
    : ILanguageModelType, IServiceType<System.Guid, TService, TFactory, TConfiguration>
    where TService : ILanguageModel
    where TConfiguration : IGenericConfiguration
    where TFactory : ILanguageModelFactory<TService, TConfiguration>
{
}
