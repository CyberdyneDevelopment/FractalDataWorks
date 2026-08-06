using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Calculations;

/// <summary>
/// Data row mapping for the <c>calc.WindowedCalculationPartitionField</c> table.
/// Each row represents a single field used to partition windowed calculation results.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record WindowedCalculationPartitionFieldRow(
    Guid RowId,
    Guid Id,
    Guid WindowedCalculationId,
    string FieldName,
    int Ordinal,
    bool IsCurrent,
    bool IsDeleted);
