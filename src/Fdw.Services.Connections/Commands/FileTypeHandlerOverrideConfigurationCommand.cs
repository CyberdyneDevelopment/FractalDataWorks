using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the FileTypeHandlerOverride configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "FileTypeHandlerOverride")]
public sealed class FileTypeHandlerOverrideConfigurationCommand : ConfigurationCommandBase<FileTypeHandlerOverrideConfiguration>
{
    /// <inheritdoc/>
    public FileTypeHandlerOverrideConfigurationCommand() : base("FileTypeHandlerOverride") { }
}
