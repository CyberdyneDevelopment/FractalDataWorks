using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Data.Abstractions.Messages;

/// <summary>
/// Message indicating that a requested container was not found in configuration.
/// </summary>
[Message("ContainerNotFound")]
[MessageOption(typeof(DataGatewayMessageCollectionBase))]
public sealed class ContainerNotFoundMessage : DataGatewayMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNotFoundMessage"/> class.
    /// </summary>
    public ContainerNotFoundMessage()
        : base(
            id: 1002,
            name: "ContainerNotFound",
            severity: MessageSeverity.Error,
            message: "Container '{0}' not found in configuration",
            code: "DG_CONTAINER_NOT_FOUND")
    {
    }
}
