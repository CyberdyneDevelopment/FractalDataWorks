using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.FileSystem.Commands;

/// <summary>ConfigurationCommands TypeOption for the FileSystemConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "FileSystemConnection")]
public sealed class FileSystemConnectionConfigurationCommand : ConfigurationCommandBase<FileSystemConnectionConfiguration>
{
    /// <summary>Initializes the command with table name 'FileSystemConnection'.</summary>
    public FileSystemConnectionConfigurationCommand() : base("FileSystemConnection") { }
}
