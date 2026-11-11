using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the FileTypeHandlerOverride configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "FileTypeHandlerOverride")]
public sealed class FileTypeHandlerOverrideConfigurationCommand : ConfigurationCommandBase<FileTypeHandlerOverrideConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FileTypeHandlerOverrideConfigurationCommand"/> class.</summary>
    public FileTypeHandlerOverrideConfigurationCommand() : base("FileTypeHandlerOverride") { }
}
