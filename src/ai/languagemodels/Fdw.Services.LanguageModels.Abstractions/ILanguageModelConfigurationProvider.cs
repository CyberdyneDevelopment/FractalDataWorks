using Fdw.Services.Abstractions;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>Resolves configured language models and routes each to the implementation provider that owns it.</summary>
public interface ILanguageModelConfigurationProvider
    : IDomainConfigurationProvider<ILanguageModelImplementationConfiguration>
{
}
