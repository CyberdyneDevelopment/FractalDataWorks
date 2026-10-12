using Fdw.Messages;

namespace Fdw.Data.DataContainers.Abstractions.Messages;

/// <summary>
/// Message indicating that write access validation failed for a container.
/// </summary>
public sealed class WriteAccessValidationFailedMessage : ContainerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteAccessValidationFailedMessage"/> class.
    /// </summary>
    public WriteAccessValidationFailedMessage()
        : base(
            id: 2002,
            name: "WriteAccessValidationFailed",
            severity: MessageSeverity.Error,
            message: "Write access validation failed for container '{0}': {1}",
            code: "CONT_WRITE_ACCESS_FAILED")
    {
    }
}
