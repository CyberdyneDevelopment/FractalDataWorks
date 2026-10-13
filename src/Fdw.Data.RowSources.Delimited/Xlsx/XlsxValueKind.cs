namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>What a numeric cell holds, as its number format says: Excel stores a date as a plain number.</summary>
internal enum XlsxValueKind
{
    /// <summary>A number, read as a whole number or a decimal.</summary>
    Number,

    /// <summary>A date or a date with a time, read as a date and time.</summary>
    DateTime,

    /// <summary>A time of day or an elapsed time.</summary>
    Time,
}
