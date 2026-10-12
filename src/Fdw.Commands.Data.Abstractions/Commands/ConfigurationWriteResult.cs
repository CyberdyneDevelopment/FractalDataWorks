using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The key values a configuration write hands back: those of every row it retired, and those of the
/// row it wrote.
/// </summary>
/// <remarks>
/// Why keys and not a row count: a row's children reach it through the Foreign key their container
/// declares, and that key holds the fields of one of this row's keys. The write is the one place those
/// values are known, so it returns them and the caller sets them on the children as field values.
/// Each entry holds the fields of every key the container declares on itself -- not a named key type,
/// and not a named column -- so whichever key a child's Foreign key references is present.
/// </remarks>
public sealed class ConfigurationWriteResult
{
    /// <summary>The column every OUTPUT row of a configuration write carries, saying which of the two it reports.</summary>
    public const string KindColumn = "WriteKind";

    /// <summary>The <see cref="KindColumn"/> value on a row a write retired.</summary>
    public const string RetiredKind = "Retired";

    /// <summary>The <see cref="KindColumn"/> value on the row a write inserted.</summary>
    public const string WrittenKind = "Written";

    /// <summary>Initializes a new instance of the <see cref="ConfigurationWriteResult"/> class.</summary>
    /// <param name="retired">The key values of every row the write retired; empty when it retired none.</param>
    /// <param name="written">The key values of the row the write inserted; <see langword="null"/> for a retire.</param>
    public ConfigurationWriteResult(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> retired,
        IReadOnlyDictionary<string, object?>? written)
    {
        Retired = retired;
        Written = written;
    }

    /// <summary>Gets the key values of every row the write retired, keyed by field name.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Retired { get; }

    /// <summary>Gets the key values of the row the write inserted, keyed by field name, or null for a retire.</summary>
    public IReadOnlyDictionary<string, object?>? Written { get; }
}
