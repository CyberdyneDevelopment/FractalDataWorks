using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>
/// One role a webhook trigger's runs act with (FDW-1884). Cascade child of <see cref="WebhookTriggerConfiguration"/>
/// (pipe.WebhookTriggerRunRole): the roles its creator chose when the trigger was created or last changed.
/// </summary>
/// <remarks>
/// Why the trigger records them: a call comes with no person behind it, so the trigger holds what its creator named. The creator had to hold
/// each role in the tenant they acted in; every call runs with exactly these roles and is judged again, when it is made, for what the
/// pipeline then reaches. The role's name, not its id, so a role that is renamed or removed stops resolving and the call is refused naming it.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "WebhookTrigger")]
public sealed partial class WebhookTriggerRunRoleConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the name of the role the trigger's runs act with.</summary>
    public string RoleName { get; set; } = string.Empty;
}
