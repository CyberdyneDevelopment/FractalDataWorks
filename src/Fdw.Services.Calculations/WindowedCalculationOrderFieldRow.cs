using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Calculations;

/// <summary>
/// Data row mapping for the <c>calc.WindowedCalculationOrderField</c> table.
/// Each row represents a single field used for ordering within windowed calculation partitions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record WindowedCalculationOrderFieldRow(
    Guid RowId,
    Guid Id,
    Guid WindowedCalculationId,
    string FieldName,
    string Direction,
    int Ordinal,
    bool IsCurrent,
    bool IsDeleted);
