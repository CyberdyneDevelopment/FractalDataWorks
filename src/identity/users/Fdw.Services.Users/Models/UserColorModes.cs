using System.Collections.Generic;

namespace Fdw.Services.Users.Models;

/// <summary>
/// The colour modes a person can choose (FDW-3061): a fixed light or dark, or following the device they are on.
/// </summary>
/// <remarks>
/// A person who has not chosen has no colour mode at all (null on <see cref="IUserPreferencesImplementationConfiguration.ColorMode"/>);
/// none of these stands for "not chosen". The values are the ones the database accepts in <c>usr.UserPreferencesConfiguration.ColorMode</c>.
/// </remarks>
public static class UserColorModes
{
    /// <summary>Always light.</summary>
    public const string Light = "light";

    /// <summary>Always dark.</summary>
    public const string Dark = "dark";

    /// <summary>Follow the device's own setting.</summary>
    public const string System = "system";

    /// <summary>Gets the modes a person can choose, in the order they are offered.</summary>
    public static IReadOnlyList<string> All { get; } = [Light, Dark, System];
}
