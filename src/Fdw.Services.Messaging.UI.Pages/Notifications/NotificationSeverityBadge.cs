namespace Fdw.Services.Notifications.UI.Pages;

/// <summary>
/// Chooses the badge class a notification severity is drawn in, for the notifications pages.
/// </summary>
/// <remarks>
/// <para>
/// The notifications pages draw their severity pills from a second badge vocabulary —
/// <c>badge-danger</c>, <c>badge-warning</c>, <c>badge-caution</c>, <c>badge-idle</c> — rather than
/// the <c>b-*</c> set every other page uses through <c>Badge</c> and <c>StatusVariants</c>. Two
/// copies of this switch had drifted apart into two page files; this is the one place it lives now.
/// </para>
/// <para>
/// It is deliberately not folded into <c>StatusVariants</c>: of the four classes only
/// <c>badge-idle</c> is defined in the reference console's stylesheet, so mapping them onto the
/// <c>b-*</c> tones would colour three pills that render uncoloured today. The vocabulary and the
/// missing definitions are the defect; this makes them visible at one address instead of two.
/// </para>
/// </remarks>
public static class NotificationSeverityBadge
{
    /// <summary>
    /// Gets the badge class for a severity name.
    /// </summary>
    /// <param name="severity">The severity name, in any casing.</param>
    /// <returns>The css class the pill is drawn with.</returns>
    public static string CssClass(string severity) =>
        severity.ToUpperInvariant() switch
        {
            "CRITICAL" => "badge-danger",
            "HIGH" or "ERROR" => "badge-warning",
            "MEDIUM" or "WARNING" => "badge-caution",
            _ => "badge-idle",
        };
}
