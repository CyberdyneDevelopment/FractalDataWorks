using Fdw.Configuration;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One configured language model: the domain record, stating which implementation it is and holding that implementation's own configuration.
/// </summary>
public interface ILanguageModelConfiguration
    : IDomainConfiguration
{
}
