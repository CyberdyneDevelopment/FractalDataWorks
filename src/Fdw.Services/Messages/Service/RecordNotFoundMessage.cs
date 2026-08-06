using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a requested record was not found.
/// </summary>
[Message("RecordNotFound")]
public sealed class RecordNotFoundMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordNotFoundMessage"/> class.
    /// </summary>
    public RecordNotFoundMessage()
        : base(1002, "RecordNotFound", MessageSeverity.Warning,
               "{0} with ID {1} was not found", "RECORD_NOT_FOUND")
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordNotFoundMessage"/> class with formatted message.
    /// The source generator will create ServiceMessages.RecordNotFound(entityType, entityId) method.
    /// </summary>
    /// <param name="entityType">The type of entity that was not found.</param>
    /// <param name="entityId">The ID of the entity that was not found.</param>
    public RecordNotFoundMessage(string entityType, object entityId)
        : base(1002, "RecordNotFound", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "{0} with ID {1} was not found", entityType, entityId),
               "RECORD_NOT_FOUND")
    { }

}
