using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Dataverses.Commands;

/// <summary>ConfigurationCommands TypeOption for the Note configuration domain.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "Note")]
public sealed class NoteConfigurationCommand : ConfigurationCommandBase<NoteConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="NoteConfigurationCommand"/> class.</summary>
    public NoteConfigurationCommand() : base("Note") { }
}
