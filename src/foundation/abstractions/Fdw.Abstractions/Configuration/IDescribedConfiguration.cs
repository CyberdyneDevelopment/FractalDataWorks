namespace Fdw.Configuration;

/// <summary>
/// States that a configuration carries a <see cref="Description"/> a provider may read or set (FDW-6208): the text is kept in the descriptions table, not in the configuration's own
/// table, so the member is never stored with the record.
/// </summary>
/// <remarks>
/// <para>
/// Why this exists: the domains that already moved their descriptions each declare <c>string? Description { get; set; }</c> on their own implementation contract (the data store,
/// connection, quality, workspace, notification and the other contracts), so nothing a provider of a polymorphic domain could ask "does this typed body carry a text" through
/// was shared. It is the one question the identity and secret manager providers need to ask of a body whose kind may or may not hold a text; the other domains are not changed to use it.
/// </para>
/// <para>
/// The text is <see langword="null"/> when a record was read (these domains never fill a record on load) or when the caller stated none. A provider that saves the record treats
/// <see langword="null"/> as "keep the stored text", a text as "write it" and an empty or blank text as "remove it".
/// </para>
/// </remarks>
public interface IDescribedConfiguration
{
    /// <summary>Gets or sets the description of the configuration, or <see langword="null"/> when none is stated.</summary>
    string? Description { get; set; }
}
