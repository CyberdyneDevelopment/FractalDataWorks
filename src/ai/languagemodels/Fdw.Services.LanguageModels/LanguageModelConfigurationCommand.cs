using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.LanguageModels;

/// <summary>
/// ConfigurationCommands TypeOption for the language model domain: produces the data commands against the domain table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "LanguageModel")]
public sealed class LanguageModelConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelConfigurationCommand"/> class.</summary>
    public LanguageModelConfigurationCommand() : base("LanguageModel") { }
}
