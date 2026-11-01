using Fdw.Messages;

namespace Fdw.Data.DataContainers.Abstractions.Messages;

/// <summary>
/// Message indicating that read access validation failed for a container.
/// </summary>
public sealed class ReadAccessValidationFailedMessage : ContainerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAccessValidationFailedMessage"/> class.
    /// </summary>
    public ReadAccessValidationFailedMessage()
        : base(
            id: 2001,
            name: "ReadAccessValidationFailed",
            severity: MessageSeverity.Error,
            message: "Read access validation failed for container '{0}': {1}",
            code: "CONT_READ_ACCESS_FAILED")
    {
    }
}
