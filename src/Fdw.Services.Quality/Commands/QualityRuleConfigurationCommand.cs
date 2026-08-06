using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the QualityRule configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "QualityRule")]
public sealed class QualityRuleConfigurationCommand : ConfigurationCommandBase<QualityRuleConfiguration>
{
    /// <inheritdoc/>
    public QualityRuleConfigurationCommand() : base("QualityRule") { }
}
