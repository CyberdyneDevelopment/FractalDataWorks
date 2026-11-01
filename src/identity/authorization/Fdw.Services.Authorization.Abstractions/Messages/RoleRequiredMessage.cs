using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Messages;

/// <summary>
/// Message indicating a required role is missing.
/// </summary>
[Message("RoleRequired")]
[MessageOption(typeof(AuthorizationMessage))]
public sealed class RoleRequiredMessage : AuthorizationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRequiredMessage"/> class.
    /// </summary>
    public RoleRequiredMessage()
        : base(3003, "RoleRequired", MessageSeverity.Warning,
               "Required role not found", "ROLE_REQUIRED")
    { }

    /// <summary>
    /// Initializes a new instance with context.
    /// </summary>
    public RoleRequiredMessage(string userId, string role)
        : base(3003, "RoleRequired", MessageSeverity.Warning,
               $"User '{userId}' requires role '{role}'", "ROLE_REQUIRED")
    { }
}
