using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Message indicating that a container name is required.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ContainerNameRequired")]
public sealed class ContainerNameRequiredMessage : DataCommandMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNameRequiredMessage"/> class.
    /// </summary>
    public ContainerNameRequiredMessage()
        : base(
            id: 2,
            name: "ContainerNameRequired",
            severity: MessageSeverity.Error,
            message: "Container name is required for data command",
            code: "DATACMD_002")
    {
    }
}
