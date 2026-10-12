namespace Fdw.Commands.Data.ValueSources;

/// <summary>A column value that is the database's current timestamp, taken once per statement.</summary>
public sealed class CurrentTimestampValueSource : ColumnValueSource
{
    /// <summary>The kind name connection types render.</summary>
    public const string KindName = "CurrentTimestamp";

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentTimestampValueSource"/> class.
    /// </summary>
    public CurrentTimestampValueSource()
        : base(KindName)
    {
    }
}
