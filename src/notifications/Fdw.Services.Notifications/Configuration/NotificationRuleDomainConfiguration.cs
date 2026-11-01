using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// The notification rules domain's record: the four properties every domain has, and everything a list of
/// notification rules shows.
/// </summary>
/// <remarks>
/// Maps to <c>notify.NotificationRule</c> in ConfigurationDb. Everything a rule is beyond these fields lives on
/// its implementation; these live here, on the domain row, so a page of rules is one read of one table. Reading
/// a rule stamps them onto its implementation; saving one whose values differ writes a new version of this row
/// from them (FDW-978, FDW-1005). The defaults are the implementation's own.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class NotificationRuleDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the rule's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the rule is in force.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the kind of notification service the rule sends through.</summary>
    public string NotificationServiceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the notification service the rule sends through.</summary>
    public string NotificationServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity the rule notifies at.</summary>
    public string Severity { get; set; } = "Info";
}
