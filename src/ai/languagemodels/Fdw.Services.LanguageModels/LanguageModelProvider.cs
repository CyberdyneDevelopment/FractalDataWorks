using Fdw.Services.Abstractions;
using Fdw.Services;
using Fdw.Services.LanguageModels.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.LanguageModels;

/// <summary>
/// Default implementation of <see cref="ILanguageModelProvider"/>. Every lookup, by name, by id or all, goes through the base, which holds no cache
/// of its own: the configuration read is cached by the configuration gateway.
/// </summary>
public sealed class LanguageModelProvider
    : DomainServiceProviderBase<
          ILanguageModel,
          ILanguageModelImplementationConfiguration,
          ILanguageModelFactory<ILanguageModel, ILanguageModelImplementationConfiguration>,
          ILanguageModelConfigurationProvider>,
      ILanguageModelProvider
{
    /// <summary>Gets how this provider reads the configuration it builds a service from.</summary>
    /// <remarks>This domain has no description fill today, so its configuration is read whole (FDW-7354).</remarks>
    protected override ConfigurationReadOptions ReadOptions => ConfigurationReadOptions.Whole;

    /// <summary>Initializes a new instance of the <see cref="LanguageModelProvider"/> class.</summary>
    /// <param name="logger">Logger instance.</param>
    public LanguageModelProvider(ILogger<LanguageModelProvider> logger)
        : base(logger ?? NullLogger<LanguageModelProvider>.Instance)
    {
    }
}
