namespace Fdw.Services.Abstractions;

/// <summary>
/// What a page read of a domain's members does beyond reading its rows, stated by the caller (FDW-6124).
/// </summary>
/// <remarks>
/// A provider that fills members of what it reads from rows of another domain (a data set's description, read from the descriptions table) does that fill on
/// every read. A caller that shows a row and not that text states <see cref="WithoutFilledMembers"/>, and the read leaves the fill out: the members come back as
/// their own rows hold them, so a list page of 580 data sets does not read 580 texts. Empty there means "not read", not "has none". A read that states
/// <see cref="Whole"/>, or a read that states nothing, fills as it always did. The two are named instances and nothing is chosen for a caller: the overload
/// without options is the read that fills.
/// </remarks>
public sealed class ConfigurationReadOptions
{
    private ConfigurationReadOptions(bool fillsMembers)
    {
        FillsMembers = fillsMembers;
    }

    /// <summary>Gets the read that fills what the records' own rows do not hold, as every read did before this option.</summary>
    public static ConfigurationReadOptions Whole { get; } = new(true);

    /// <summary>Gets the read that leaves out what a read observer fills from other domains.</summary>
    public static ConfigurationReadOptions WithoutFilledMembers { get; } = new(false);

    /// <summary>Gets whether the read fills what the records' own rows do not hold.</summary>
    public bool FillsMembers { get; }
}
