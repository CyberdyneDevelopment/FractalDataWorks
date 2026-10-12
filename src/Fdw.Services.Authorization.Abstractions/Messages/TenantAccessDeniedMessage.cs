using System;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Messages;

/// <summary>
/// Message indicating tenant access was denied.
/// </summary>
[Message("TenantAccessDenied")]
[MessageOption(typeof(AuthorizationMessage))]
public sealed class TenantAccessDeniedMessage : AuthorizationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAccessDeniedMessage"/> class.
    /// </summary>
    public TenantAccessDeniedMessage()
        : base(3004, "TenantAccessDenied", MessageSeverity.Warning,
               "Tenant access denied", "TENANT_DENIED")
    { }

    /// <summary>
    /// Initializes a new instance with context.
    /// </summary>
    public TenantAccessDeniedMessage(string userId, Guid tenantId)
        : base(3004, "TenantAccessDenied", MessageSeverity.Warning,
               $"User '{userId}' denied access to tenant '{tenantId}'", "TENANT_DENIED")
    { }
}
