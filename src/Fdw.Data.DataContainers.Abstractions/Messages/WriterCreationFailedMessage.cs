using Fdw.Messages;

namespace Fdw.Data.DataContainers.Abstractions.Messages;

/// <summary>
/// Message indicating that writer creation failed for a container.
/// </summary>
public sealed class WriterCreationFailedMessage : ContainerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriterCreationFailedMessage"/> class.
    /// </summary>
    public WriterCreationFailedMessage()
        : base(
            id: 2005,
            name: "WriterCreationFailed",
            severity: MessageSeverity.Error,
            message: "Failed to create writer for container '{0}': {1}",
            code: "CONT_WRITER_FAILED")
    {
    }
}
