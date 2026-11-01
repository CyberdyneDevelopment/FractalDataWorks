using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Messages;

/// <summary>
/// Message indicating insufficient permissions for an operation.
/// </summary>
[Message("InsufficientPermissions")]
[MessageOption(typeof(AuthorizationMessage))]
public sealed class InsufficientPermissionsMessage : AuthorizationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientPermissionsMessage"/> class.
    /// </summary>
    public InsufficientPermissionsMessage()
        : base(3002, "InsufficientPermissions", MessageSeverity.Warning,
               "Insufficient permissions", "INSUFFICIENT_PERMS")
    { }

    /// <summary>
    /// Initializes a new instance with context.
    /// </summary>
    public InsufficientPermissionsMessage(string userId, string permission)
        : base(3002, "InsufficientPermissions", MessageSeverity.Warning,
               $"User '{userId}' lacks permission '{permission}'", "INSUFFICIENT_PERMS")
    { }
}
