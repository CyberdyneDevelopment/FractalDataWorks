using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Calculations;

/// <summary>
/// Data record mapping for the <c>calc.WindowedCalculationPartitionField</c> table.
/// Each record represents a single field used to partition windowed calculation results.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class WindowedCalculationPartitionFieldRecord
{

    /// <summary>Gets or sets the logical identity of this partition field.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent windowed calculation identifier.</summary>
    public Guid WindowedCalculationId { get; set; }



    /// <summary>Gets or sets the ordinal position of this partition field.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets the soft delete flag.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the user who created this record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the user who last modified this record.</summary>
    public string ModifyBy { get; set; } = string.Empty;
}
