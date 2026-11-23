namespace Fdw.Services.Messaging;

/// <summary>
/// The limits on a question about alerts (FDW-2420, FDW-7059): how many alerts one question may name.
/// </summary>
/// <remarks>
/// Configuration, never code: the default is declared in the messaging service type option's declared defaults (<c>MaxAlerts</c>), a host changes it at
/// registration with <c>ServiceTypeDefaults.Override</c>, and a value stated under <c>Messaging:Default:Alerts</c> wins. The registration builds one of these from what applies.
/// </remarks>
public sealed class AlertLimits
{
    /// <summary>Initializes a new instance of the <see cref="AlertLimits"/> class.</summary>
    /// <param name="maxAlerts">The most alerts one question may name.</param>
    public AlertLimits(int maxAlerts)
    {
        MaxAlerts = maxAlerts;
    }

    /// <summary>Gets the most alerts one question may name.</summary>
    public int MaxAlerts { get; }
}
