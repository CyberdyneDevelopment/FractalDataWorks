namespace Fdw.Services.Messaging;

/// <summary>
/// The limits on a question about alerts (FDW-2420).
/// </summary>
public static class AlertLimits
{
    /// <summary>The most alerts one question may name.</summary>
    public const int MaxAlerts = 100;
}
