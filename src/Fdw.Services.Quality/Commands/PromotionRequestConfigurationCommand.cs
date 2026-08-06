using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the PromotionRequest configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "PromotionRequest")]
public sealed class PromotionRequestConfigurationCommand : ConfigurationCommandBase<PromotionRequestConfiguration>
{
    /// <inheritdoc/>
    public PromotionRequestConfigurationCommand() : base("PromotionRequest") { }
}
