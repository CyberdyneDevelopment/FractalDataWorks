namespace Fdw.Commands.Data.ValueSources;

/// <summary>
/// A column value that is a new UUIDv7, minted for each row written. It is a description: the engine mints it
/// itself for the rows of an insert, and a connection type renders it in its own dialect for an upsert.
/// </summary>
public sealed class UuidV7ValueSource : ColumnValueSource
{
    /// <summary>The kind name connection types render.</summary>
    public const string KindName = "UuidV7";

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidV7ValueSource"/> class.
    /// </summary>
    public UuidV7ValueSource()
        : base(KindName)
    {
    }
}
