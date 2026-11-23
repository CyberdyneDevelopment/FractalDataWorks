using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Globalization;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A date written in ISO 8601.</summary>
[TypeOption(typeof(XlsxCellTypes), "IsoDate")]
[ExcludeFromCodeCoverage]
public sealed class IsoDateXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="IsoDateXlsxCellType"/> class.</summary>
    public IsoDateXlsxCellType()
        : base("IsoDate", "d")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment)
        ? moment
        : throw new InvalidDataException($"'{text}' is not an ISO 8601 date.");
}
