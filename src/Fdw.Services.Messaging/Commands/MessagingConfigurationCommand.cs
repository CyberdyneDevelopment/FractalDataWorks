using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Messaging.Commands;

/// <summary>
/// The configuration command for <see cref="MessagingConfiguration"/> rows.
/// </summary>
/// <remarks>
/// The string handed to the base IS the container the provider queries —
/// <c>ConfigurationCommandBase</c> exposes <c>ContainerName =&gt; TableName</c> — so it has to match
/// the container declared in configurationSchema.json exactly, and the option key with it.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "Messaging")]
public sealed class MessagingConfigurationCommand : ConfigurationCommandBase<MessagingConfiguration>
{
    /// <inheritdoc/>
    public MessagingConfigurationCommand() : base("Messaging") { }
}
