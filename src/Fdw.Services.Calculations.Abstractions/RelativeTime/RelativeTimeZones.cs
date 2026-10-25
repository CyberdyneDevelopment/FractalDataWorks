using System;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Finds the time zone a relative-time node states. None is assumed: a node that names no zone, or one this server does not know, is
/// refused when the calculation is saved.
/// </summary>
public static class RelativeTimeZones
{
    /// <summary>Looks a time zone up by its id.</summary>
    /// <param name="id">The id, an IANA name such as <c>America/Chicago</c> or <c>UTC</c>.</param>
    /// <param name="zone">The zone, or null when the id is not known.</param>
    /// <param name="reason">Why the id is not known, or an empty text when it is.</param>
    /// <returns>True when the zone was found.</returns>
    public static bool TryFind(string id, out TimeZoneInfo? zone, out string reason)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            reason = string.Empty;
            return true;
        }
        catch (TimeZoneNotFoundException exception)
        {
            zone = null;
            reason = exception.Message;
            return false;
        }
        catch (InvalidTimeZoneException exception)
        {
            zone = null;
            reason = exception.Message;
            return false;
        }
    }
}
