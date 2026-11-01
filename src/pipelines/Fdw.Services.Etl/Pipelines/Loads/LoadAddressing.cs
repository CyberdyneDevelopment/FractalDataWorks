using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Where a destination writes when it states a file or files and not the container as declared (FDW-3193): the target kind, and the physical path and
/// name or the field to split by, as the destination stated them.
/// </summary>
/// <param name="Kind">The kind: File or Files.</param>
/// <param name="PhysicalPath">The path to write at in place of the declared one, or <see langword="null"/>.</param>
/// <param name="PhysicalName">The file to write in place of the declared one (File), or <see langword="null"/>.</param>
/// <param name="SplitField">The field whose value names the file each row goes to (Files), or <see langword="null"/>.</param>
public sealed record LoadAddressing(ITargetKind Kind, string? PhysicalPath, string? PhysicalName, string? SplitField)
{
    /// <summary>Gets a value indicating whether each row goes to the file its split value names.</summary>
    public bool Splits => ReferenceEquals(Kind, TargetKinds.Files);

    /// <summary>Reads what a destination states about where it writes; none for the kinds that write the container as declared.</summary>
    /// <param name="targetKind">The name of the kind the destination states, or <see langword="null"/> for none.</param>
    /// <param name="physicalPath">The physical path it states.</param>
    /// <param name="physicalName">The physical name it states.</param>
    /// <param name="splitField">The split field it states.</param>
    /// <returns>The addressing, or <see langword="null"/> when the destination writes the container as declared.</returns>
    public static LoadAddressing? Of(string? targetKind, string? physicalPath, string? physicalName, string? splitField)
        => targetKind is { } named && TargetKinds.ByName(named) is var kind
            && (ReferenceEquals(kind, TargetKinds.File) || ReferenceEquals(kind, TargetKinds.Files))
            ? new LoadAddressing(kind, physicalPath, physicalName, splitField)
            : null;
}
