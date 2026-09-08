using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Dataverses.Commands;

/// <summary>ConfigurationCommands TypeOption for the Dataverse configuration domain.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "Dataverse")]
public sealed class DataverseConfigurationCommand : ConfigurationCommandBase<DataverseConfiguration>
{
    /// <inheritdoc/>
    public DataverseConfigurationCommand() : base("Dataverse") { }
}
