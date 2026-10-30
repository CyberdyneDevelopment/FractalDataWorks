using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// The notifications domain's record: the four properties every domain has, and the fields a notification
/// channel keeps on its domain row.
/// </summary>
/// <remarks>
/// Maps to <c>notify.Notification</c> in ConfigurationDb. Whether a channel is in force and which secret it reads
/// live here, on the domain row, not in the table of its kind (FDW-1158). Reading a channel stamps them onto its
/// implementation; saving one writes them to this row from the implementation (FDW-978, FDW-1005, FDW-1182). Until
/// this record declared them the domain kept the plain four-property record, so the row was written without
/// <c>IsEnabled</c> and every channel create was refused by the table. The values are the implementation's own: this
/// record states none, so a channel is never enabled by a default found here.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class NotificationDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets whether the channel is in force.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the name of the secret manager the channel's secret is read from, when it has one.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the name of the secret the channel reads from its secret manager, when it has one.</summary>
    public string? SecretKeyName { get; set; }

    /// <summary>Gets or sets what the notification is for, kept on the domain row.</summary>
    public string? Description { get; set; }
}
