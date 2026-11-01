namespace Fdw.Services.Abstractions;

/// <summary>
/// How deep a configuration read composes the aggregate under the row it reads.
/// </summary>
/// <remarks>
/// A level is one hop down the child cascade: a data store's paths are one level under it, their
/// containers two, the containers' fields and keys three. Every child collection costs a query per
/// owner row, so a list that composes whole aggregates costs a query per row per collection at every
/// depth. A caller that shows a row and not its children asks for <see cref="RecordOnly"/> and pays
/// for one query; a caller that needs the tree asks for <see cref="WholeAggregate"/>.
/// <para>
/// A collection below the requested depth is left empty. Empty there means "not read", not "has
/// none": a caller that asked for fewer levels must not report a count from it.
/// </para>
/// </remarks>
public static class ConfigurationLevels
{
    /// <summary>The row alone; no child collection is read.</summary>
    public const int RecordOnly = 0;

    /// <summary>The row and its direct child collections, each child read as a record only.</summary>
    public const int Children = 1;

    /// <summary>The row and every collection under it, at every depth.</summary>
    public const int WholeAggregate = int.MaxValue;
}
