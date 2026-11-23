using Fdw.ServiceTypes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Provider for configured language models. <c>Get(name)</c>, <c>Get(id)</c> and <c>Get()</c> are inherited from the domain service provider.
/// </summary>
public interface ILanguageModelProvider : IDomainServiceProvider<ILanguageModel, ILanguageModelImplementationConfiguration>
{
}
