using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the QualityRule configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "QualityRule")]
public sealed class QualityRuleConfigurationCommand : ConfigurationCommandBase<QualityRuleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QualityRuleConfigurationCommand"/> class.</summary>
    public QualityRuleConfigurationCommand() : base("QualityRule") { }
}
