using System.Collections.Generic;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// The member names every generated collection defines for itself, which a <c>[TypeOption]</c> name
/// therefore cannot take.
/// </summary>
/// <remarks>
/// <para>
/// A collection's generated body draws identifiers from two places: fixed names the generator owns,
/// and names derived from what the option author wrote. Where those two meet, the C# compiler
/// reports a duplicate member inside a file the author never wrote and cannot open — so the meeting
/// points are worth being deliberate about.
/// </para>
/// <para>
/// Private state is kept apart by construction: the singleton backing fields are emitted as
/// <c>_option{PascalCase(name)}</c>, which no fixed field name matches. An option may therefore be
/// called <c>Lock</c> or <c>Metadata</c> freely, which matters because options are
/// downstream-extensible — a consumer adds <c>[TypeOption]</c> against an FDW collection in its own
/// assembly, and a name that reads well there should not be vetoed by an FDW-internal field name it
/// cannot see.
/// </para>
/// <para>
/// The public accessor is the case that admits no such trick. An option named <c>All</c> wants the
/// member <c>All</c>, and the collection already exposes <c>All()</c>; renaming either one would
/// take away the API someone is asking for. That is a genuine authoring conflict, so it is reported
/// as <c>TC012</c> against the option's own declaration.
/// </para>
/// <para>
/// The list holds only members emitted unconditionally. <c>Count</c> is deliberately absent: it is
/// emitted by some collection variants but not all, and an option named <c>Count</c> already exists
/// and compiles. Adding a conditional name here would reject code that works today.
/// </para>
/// </remarks>
public static class ReservedMemberNames
{
    /// <summary>
    /// Member names generated on every collection variant.
    /// </summary>
    public static readonly IReadOnlyCollection<string> All = new HashSet<string>(
        new[]
        {
            "All",
            "ByCategory",
            "ById",
            "ByName",
            "Categories",
            "GetMetadata",
            "NotFound",
            "RegisterMember",
        },
        System.StringComparer.Ordinal);

    /// <summary>
    /// Determines whether an option name would collide with a generated member.
    /// </summary>
    /// <param name="optionName">The name given in the <c>[TypeOption]</c> attribute.</param>
    /// <returns><see langword="true"/> when the name is reserved.</returns>
    public static bool IsReserved(string optionName) =>
        optionName is not null && ((HashSet<string>)All).Contains(optionName);
}
