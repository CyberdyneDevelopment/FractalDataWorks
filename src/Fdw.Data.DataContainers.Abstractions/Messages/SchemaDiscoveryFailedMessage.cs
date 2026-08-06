using Fdw.Messages;

namespace Fdw.Data.DataContainers.Abstractions.Messages;

/// <summary>
/// Message indicating that schema discovery failed for a container.
/// </summary>
public sealed class SchemaDiscoveryFailedMessage : ContainerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryFailedMessage"/> class.
    /// </summary>
    public SchemaDiscoveryFailedMessage()
        : base(
            id: 2003,
            name: "SchemaDiscoveryFailed",
            severity: MessageSeverity.Error,
            message: "Schema discovery failed for container '{0}': {1}",
            code: "CONT_SCHEMA_DISC_FAILED")
    {
    }
}
